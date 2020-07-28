///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using InnerEye.CreateDataset.Common;
    using InnerEye.CreateDataset.Common.Models;
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;
    using MoreLinq;

    /// <summary>
    /// Class to hold two structure names and an operation name, and apply the operation
    /// to structures with those names if present.
    /// </summary>
    public class StructureOperation
    {
        /// <summary>
        /// Name of the first structure (on the left of the operator)
        /// </summary>
        public string StructureName1 { get; }
        /// <summary>
        /// Name of the operator
        /// </summary>
        public StructureOperationName OperationName { get; }
        /// <summary>
        /// Name of the second structure (on the right of the operator)
        /// </summary>
        public string StructureName2 { get; }

        public StructureOperation(string name1, StructureOperationName opName, string name2)
        {
            StructureName1 = name1;
            OperationName = opName;
            StructureName2 = name2;
        }

        /// <summary>
        /// Attempt to create a StructureOperation from a string of the form "AOB", where A and B are names
        /// containing only alphanumerics, space and underscore, and O is one of the keys in the "operations"
        /// dictionary below. If the string is of the right form, a StructureOperation is returned; otherwise null.
        /// </summary>
        public static StructureOperation FromString(string expr)
        {
            var operations = new Dictionary<string, StructureOperationName> {
                    { "gt", StructureOperationName.Above },
                    { "ge", StructureOperationName.NotBelow },
                    { "lt", StructureOperationName.Below },
                    { "le", StructureOperationName.NotAbove },
                    { "intersection", StructureOperationName.Intersection },
                    { "union", StructureOperationName.Union },
                    { "minus", StructureOperationName.Minus },
                };
            var fields = expr.Split('.');
            if (fields.Length == 3 && operations.ContainsKey(fields[1]))
                {
                    return new StructureOperation(fields[0], operations[fields[1]], fields[2]);
                }
            return null;
        }

        /// <summary>
        /// Returns the Volume3D resulting from applying the OperationName to the two structures
        /// keyed by StructureName1 and StructureName2 in "structures"; these must both exist.
        /// The resulting Volume3D can be all-zero.
        /// </summary>
        /// <param name="structures"></param>
        /// <returns></returns>
        public Volume3D<byte> Apply(Dictionary<string, Volume3D<byte>> structures)
        {
            var volume1 = structures[StructureName1];
            var volume2 = structures[StructureName2];
            var region1 = volume1.GetInterestRegion();
            var region2 = volume2.GetInterestRegion();
            // First deal with the "above" and "below" operations. These can be handled by
            // cropping volume1 to be above or below everything in volume2.
            //
            // Lowest slice that we'll zero for the result
            int clearMin = region1.MinimumZ;
            // Highest slice that we'll zero for the result.
            int clearMax = region1.MaximumZ;
            switch (OperationName)
            {
                case StructureOperationName.Above:
                    clearMax = region2.MaximumZ;
                    break;
                case StructureOperationName.NotBelow:
                    clearMax = region2.MaximumZ - 1;
                    break;
                case StructureOperationName.Below:
                    clearMin = region2.MinimumZ;
                    break;
                case StructureOperationName.NotAbove:
                    clearMin = region2.MinimumZ + 1;
                    break;
                default:
                    clearMax = -1; // so we don't try to apply clearMin and clearMax
                    break;
            }
            var result = volume1.Copy();
            if (clearMax >= 0)
            { 
                for (var x = region1.MinimumX; x <= region1.MaximumX; x++)
                {
                    for (var y = region1.MinimumY; y <= region1.MaximumY; y++)
                    {
                        for (var z = clearMin; z <= clearMax; z++)
                        {
                            result[x, y, z] = 0;
                        }
                    }
                }
                return result;
            }
            // We have a non-cropping operation, i.e. a set operation.
            // The function to be applied at each voxel within the region.
            Func<int, byte> perVoxelComputation = null;
            // The region we'll need to compute per-voxel results over. Since "result" is preset
            // to volume1, this is the region in which result might have to differ from volume1.
            // For intersection and difference, this is region1; for union, it's region2.
            Region3D<int> computationRegion = null;
            switch (OperationName)
            {
                case StructureOperationName.Intersection:
                    computationRegion = region1;
                    perVoxelComputation = index => (byte)(volume1[index] & volume2[index]);
                    break;
                case StructureOperationName.Union:
                    computationRegion = region2;
                    perVoxelComputation = index => (byte)(volume1[index] | volume2[index]);
                    break;
                case StructureOperationName.Minus:
                    computationRegion = region1;
                    perVoxelComputation = index => (byte)(volume1[index] & (1 - volume2[index]));
                    break;
                default:
                    throw new ArgumentException($"Unexpected StructureOperationName {OperationName}");
            }
            // At every point within the intersection of the two regions, set the result voxel according
            // to the computation and the input volumes.
            Parallel.For(computationRegion.MinimumZ, computationRegion.MaximumZ + 1, z =>
            {
                for (var y = computationRegion.MinimumY; y <= computationRegion.MaximumY; y++)
                {
                    var minIndex = volume1.GetIndex(computationRegion.MinimumX, y, z);
                    var maxIndex = minIndex + computationRegion.MaximumX - computationRegion.MinimumX;
                    for (var index = minIndex; index <= maxIndex; index++)
                    {
                        result[index] = perVoxelComputation(index);
                    }
                }
            });
            return result;
        }
    }

    public enum StructureOperationName
    {
        /// <summary>
        /// "Renaming" operations that actually create new structures from pairs of old ones.
        ///  Above: A.gt.B means all voxels in A strictly above top slice of B
        ///  NotBelow: A.ge.B means all voxels in A above or in top slice of B (i.e. no voxel in A is below any voxel in B)
        ///  Below: A.lt.B means all voxels in A strictly below bottom slice of B
        ///  NotAbove: A&.le.B means all voxels in A below or in bottom slice of B (i.e. no voxel in A is above any voxel in B)
        ///  Intersection: A.intersection.B means all voxels that are in both A and B
        ///  Union: A.union.B means all voxels that are in A or B or both
        ///  Minus: A.minus.B means all voxels that are in A but not in B
        /// </summary>
        Above,
        NotBelow,
        Below,
        NotAbove,
        Intersection,
        Union,
        Minus
    }

    /// <summary>
    /// Holds the per-channel information for a given subject: A scan volume, and the associated labelled structures
    /// as a binary mask.
    /// </summary>
    public class VolumeAndStructures
    {
        private Dictionary<string, Volume3D<byte>> _structures;

        /// <summary>
        /// The volume containing the scan.
        /// </summary>
        public Volume3D<short> Volume { get; }

        /// <summary>
        /// The anatomical structures that are labelled in the volume, as a mapping between structure name and the mask volume.
        /// </summary>
        public IReadOnlyDictionary<string, Volume3D<byte>> Structures
        {
            get => _structures;
        }

        /// <summary>
        /// The metadata (subject, series, etc) that is available for the volume and structures.
        /// </summary>
        public VolumeMetadata Metadata { get; }

        /// <summary>
        /// Creates a new instance of the class, setting all properties.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="structures"></param>
        /// <param name="metadata"></param>
        public VolumeAndStructures(Volume3D<short> volume, Dictionary<string, Volume3D<byte>> structures, VolumeMetadata metadata)
        {
            Volume = volume ?? throw new ArgumentNullException(nameof(volume));
            _structures = structures ?? throw new ArgumentNullException(nameof(structures));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        /// <summary>
        /// Splits the medical volume in the argument into the actual scan, 
        /// the structures as binary masks, and the metadata.
        /// </summary>
        /// <param name="volumeAndMetadata"></param>
        /// <param name="isLowerCaseConversionEnabled">If true, all structure names are converted to lower case.</param>
        /// <returns></returns>
        public static VolumeAndStructures FromMedicalVolume(VolumeAndMetadata volumeAndMetadata,
            bool isLowerCaseConversionEnabled, bool dropRepeats = false)
        {
            var volume = volumeAndMetadata.Volume.Volume;
            var structures = new Dictionary<string, Volume3D<byte>>();
            volumeAndMetadata.Volume.Struct.Contours
            .ForEach(contour =>
            {
                var name = contour.StructureSetRoi.RoiName;
                if (isLowerCaseConversionEnabled)
                {
                    name = name.ToLowerInvariant();
                }

                if (structures.ContainsKey(name))
                {
                    if (!dropRepeats)
                    {
                        var series = volumeAndMetadata.Metadata.SeriesId;
                        throw new ArgumentException($"The volume contains multiple contours with the name '{name}'. The culprit is series {series}");
                    }
                }
                else
                {
                    structures.Add(name, contour.Contours.ToVolume3D(volume));
                }
            });

            return new VolumeAndStructures(volume, structures, volumeAndMetadata.Metadata);
        }

        /// <summary>
        /// If oldName is of the form "AOB" where A and B are structure names and O is an operation name, try to create
        /// a structure named newName by applying the operator to the A and B structures if they exists. Otherwise,
        /// if there is a structure with the given oldName, it is removed and re-added to the set of structures
        /// under the newName. If no structure with the given oldName exists, no change is made.
        /// Returns true if a change is made, false otherwise.
        /// Throws an <see cref="InvalidOperationException"/> if a structure with the oldName exists, but another
        /// structures with the given newName already exists.
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <param name="allowNameClashes">If true, a rename operation can overwrite an existing structure.</param>
        /// <returns>Returns true if a change is made or if oldName equals newName and such a structure exists (i.e.
        /// a trivial renaming), false otherwise.</returns>
        public bool RenameOrAugment(string oldName, string newName, bool allowNameClashes, bool isAugmentation)
        {
            // First, try to parse oldName as two names separated by an operator. If this succeeds and the two structures
            // exist, (try to) carry out the operation.
            var op = StructureOperation.FromString(oldName);
            var subjectId = Metadata.SubjectId;
            if (op != null && _structures.ContainsKey(op.StructureName1) && _structures.ContainsKey(op.StructureName2))
            {
                Volume3D<byte> computedStructure = op.Apply(_structures);
                if (isAugmentation)
                {
                    int nComputed = 0;
                    foreach (var b in computedStructure.Array)
                    {
                        nComputed += b;
                    }
                    Trace.TraceInformation($"Subject {subjectId}: computed structure from {oldName} has {nComputed} voxels");
                    AugmentStructureWithName(newName, computedStructure);
                    DiminishStructureWithName(op.StructureName1, computedStructure);
                    return false;  // always continue caller after augmenting
                }
                else
                {
                    MayRemoveOrThrow(oldName, newName, allowNameClashes);
                    _structures.Add(newName, computedStructure);
                    Trace.TraceInformation($"Subject {subjectId}: created {newName} by applying {op.OperationName} to {op.StructureName1} and {op.StructureName2}");
                    return true;
                }
            }
            // Treat oldName as a simple structure name.
            if (Structures.TryGetValue(oldName, out var volume))
            {
                if (newName != oldName)
                {
                    if (isAugmentation)
                    {
                        AugmentStructureWithName(newName, Structures[oldName]);
                        return false;  // always continue caller after augmenting
                    }
                    else
                    {
                        MayRemoveOrThrow(oldName, newName, allowNameClashes);
                        _structures.Remove(oldName);
                        _structures.Add(newName, volume);
                        Trace.TraceInformation($"Subject {subjectId}: renamed {oldName} to {newName}");
                    }
                }
                return true;
            }
            return false;
        }

        private void AugmentStructureWithName(string newName, Volume3D<byte> computedStructure)
        {
            var subjectId = Metadata.SubjectId;
            if (_structures.ContainsKey(newName))
            {
                int nAdded = 0;
                int nAlready = 0;
                var computedArray = computedStructure.Array;
                var target = _structures[newName];
                var targetArray = target.Array;
                for (var index = 0; index < computedArray.Length; index++)
                {
                    if (targetArray[index] == 0)
                    {
                        targetArray[index] = computedArray[index];
                        nAdded += computedArray[index];
                    } else
                    {
                        nAlready++;
                    }
                }
                Trace.TraceInformation($"Subject {subjectId}: added {nAdded} voxels to {newName} (on top of original {nAlready})");
            }
            else
            {
                Trace.TraceInformation($"Subject {subjectId}: ContainsKey failed on {newName}; keys are {string.Join(",", _structures.Keys)}");
                _structures.Add(newName, computedStructure);
                Trace.TraceInformation($"Subject {subjectId}: created {newName} as an augmentation; keys are now {string.Join(",", _structures.Keys)}");
            }
        }

        private void DiminishStructureWithName(string newName, Volume3D<byte> computedStructure)
        {
            var computedArray = computedStructure.Array;
            var target = _structures[newName];
            var targetArray = target.Array;
            int nSubtracted = 0;
            int nLeft = 0;
            for (var index = 0; index < computedArray.Length; index++)
            {
                if (computedArray[index] > 0)
                {
                    nSubtracted += targetArray[index];
                    targetArray[index] = 0;
                } else
                {
                    nLeft += targetArray[index];
                }

            }
            Trace.TraceInformation($"Subject {Metadata.SubjectId}: subtracted {nSubtracted} voxels from {newName}, leaving {nLeft}");
        }

        /// <summary>
        /// If newName occurs in Structures, either remove that structure if allowNameClashes is true, or throw
        /// an exception.
        /// </summary>
        /// <param name="oldName">name of structure to be renamed - only used in the exception</param>
        /// <param name="newName">target structure name</param>
        /// <param name="allowNameClashes">whether to go ahead anyway if structure "newName" exists</param>
        private void MayRemoveOrThrow(string oldName, string newName, bool allowNameClashes)
        {
            if (Structures.ContainsKey(newName))
            {
                if (allowNameClashes)
                {
                    _structures.Remove(newName);
                }
                else
                {
                    throw new InvalidOperationException($"Unable to perform the renaming from '{oldName}' to '{newName}': A structure with this name already exists.");
                }
            }
        }

        /// <summary>
        /// Attempts to perform all structure re-naming operations that are suggested by the argument.
        /// For a name mapping oldName1,oldName2,...:newName:
        /// (1) If zero or one structure with any of the old or new names exists, nothing is done;
        /// (2) else if allowNameClashes is false, throws an <see cref="InvalidOperationException"/> (if throwIfInvalid is true)
        /// or makes no changes and returns false (if throwIfInvalid is false)
        /// (3) else rename structures according to the description on the <see cref="AllowNameClashes"> parameter in <see cref="CommandlineCreateDataset">.
        /// 
        /// Alternatively, each "oldNameN" may be two names separated by one of the operators defined in the StructureOperation class. In this case,
        /// if structures exist for both names, the new structure is created by applying the operator to those structures (which are not removed).
        /// </summary>
        /// <param name="nameMappings"></param>
        /// <param name="allowNameClashes">If true, clashes between multiple named structures are resolved.</param>
        /// <param name="throwIfInvalid">If true, we throw if a renaming fails; otherwise we print an error and eventually return false.</param>
        /// <returns>whether the renamings (if any) succeeded</returns>
        public bool Rename(IEnumerable<NameMapping> nameMappings, bool allowNameClashes, bool throwIfInvalid = true)
        {
            bool allMappingsSuccessful = true;
            if (nameMappings != null)
            {
                foreach (var nameMapping in nameMappings)
                {
                    bool thisMappingIsValid = allowNameClashes || MappingCanBeApplied(nameMapping, throwIfInvalid);
                    if (thisMappingIsValid)
                    {
                        foreach (var oldName in nameMapping.OldNames)
                        {
                            var wasRenamed = RenameOrAugment(oldName, nameMapping.NewName, allowNameClashes, nameMapping.IsAugmentation);
                            if (wasRenamed)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        allMappingsSuccessful = false;
                    }
                }
            }
            return allMappingsSuccessful;
        }

        private bool MappingCanBeApplied(NameMapping nameMapping, bool throwIfInvalid)
        {
            if (nameMapping.IsAugmentation)
            {
                return true;
            }
            bool canBeApplied = true;
            var allNames = new List<string>(nameMapping.OldNames);
            allNames.Add(nameMapping.NewName);
            var foundNames = allNames.Where(name => Structures.ContainsKey(name)).ToList();
            if (foundNames.Count > 1)
            {
                var message = $"Subject {Metadata.SubjectId}: unable to perform the renaming from '{string.Join(",", nameMapping.OldNames)}' to '{nameMapping.NewName}': "
                    + $"structures with names {string.Join(",", foundNames)} already exist.";
                if (throwIfInvalid)
                {
                    throw new InvalidOperationException(message);
                }
                else
                {
                    Trace.TraceError(message);
                    canBeApplied = false;
                }
            }
            return canBeApplied;
        }

        /// <summary>
        /// Checks if all structures given in the argument are present. If they are not present, an all-empty
        /// mask will be added for the structure in question.
        /// </summary>
        /// <param name="structuresToAdd"></param>
        public void AddEmptyStructures(IEnumerable<string> structuresToAdd)
        {
            if (structuresToAdd == null)
            {
                return;
            }
            foreach (var name in structuresToAdd)
            {
                if (!Structures.ContainsKey(name))
                {
                    _structures.Add(name, Volume.CreateSameSize<byte>());
                }
            }
        }

        /// <summary>
        /// Adds a structure with the given name and mask. Throws an <see cref="InvalidOperationException"/>
        /// if a structure of that name is already present.
        /// </summary>
        /// <param name="structureName">The name of the structure to add.</param>
        /// <param name="volume">The binary mask that represents the structure.</param>
        public void Add(string structureName, Volume3D<byte> volume)
        {
            if (_structures.ContainsKey(structureName))
            {
                throw new InvalidOperationException($"There is already a structure with name '{structureName}'");
            }

            _structures.Add(structureName, volume);
        }

        /// <summary>
        /// Removes the structure with the given name. Throws an <see cref="InvalidOperationException"/>
        /// if a structure of that name is not already present.
        /// </summary>
        /// <param name="structureName">The name of the structure to add.</param>
        /// <param name="volume">The binary mask that represents the structure.</param>
        public void Remove(string structureName)
        {
            if (!_structures.ContainsKey(structureName))
            {
                throw new InvalidOperationException($"There is no structure with name '{structureName}'");
            }

            _structures.Remove(structureName);
        }

        /// <summary>
        /// Creates a new instance of <see cref="VolumeAndStructures"/> where image resampling has been applied to both
        /// the image volume and all of the structures, to achieve the given voxel spacing.
        /// If the voxel spacing is missing or has no elements, the present object is returned unchanged.
        /// </summary>
        /// <param name="spacingMillimeters">The desired voxel spacing that the result should have.</param>
        /// <returns></returns>
        public VolumeAndStructures GeometricNormalization(double[] spacingMillimeters)
        {
            if (spacingMillimeters.IsNullOrEmpty())
            {
                return this;
            }

            if (spacingMillimeters.Length != 3)
            {
                throw new ArgumentException("Spacing must be given with exactly 3 values.", nameof(spacingMillimeters));
            }

            var geoNorm = new GeometricNormalizationParameters
            {
                StandardiseSpacings = spacingMillimeters,
                MedianFilterRadius = 0
            };
            var newVolume = CreateDataset.GeometricNormalization.StandardiseLinear(Volume, geoNorm);
            var newStructures = new Dictionary<string, Volume3D<byte>>();
            Structures.ForEach(keyValue => 
                newStructures.Add(keyValue.Key, CreateDataset.GeometricNormalization.StandardiseNearest(keyValue.Value, geoNorm)));
            return new VolumeAndStructures(newVolume, newStructures, Metadata);
        }
    }
}
