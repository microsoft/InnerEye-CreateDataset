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
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Reflection;
    using CommandLine;
    using InnerEye.CreateDataset.Common;
    using MoreLinq;
    using System.Globalization;
    using Newtonsoft.Json;

    /// <summary>
    /// Contains information for re-naming structures. 
    /// </summary>
    public class NameMapping
    {
        /// <summary>
        /// All structures in the dataset that have any of the names given here will be affected.
        /// </summary>
        public List<string> OldNames { get; set; }

        /// <summary>
        /// All structures in the dataset that have any of the <see cref="OldNames"/> will be given this new name.
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// Whether this mapping specifies that voxels in OldNames should be added to structure NewName (expression will
        /// contain ":+" rather than ":".
        /// </summary>
        public bool IsAugmentation { get; set; }

        public NameMapping(string newName, IEnumerable<string> oldNames, bool isAugmentation=false)
        {
            NewName = newName;
            OldNames = oldNames.ToList();
            IsAugmentation = isAugmentation;
        }

        /// <summary>
        /// Removes any names containing the string "resected" (case-insensitive) from OldNames, and returns whether any were removed.
        /// </summary>
        /// <returns></returns>
        public bool DropOldNamesContaining(string substring)
        {
            substring = substring.ToLower(CultureInfo.InvariantCulture);
            var oldCount = OldNames.Count;
            OldNames = OldNames.Where(name => name.ToLower(CultureInfo.InvariantCulture).IndexOf(substring) < 0).ToList();
            return OldNames.Count != oldCount;
        }
    }

    /// <summary>
    /// Describes the ways how two binary masks can be combined to create a derived structure.
    /// </summary>
    public enum DerivedStructureOperator
    {
        /// <summary>
        /// The derived structure is the union of the two arguments.
        /// </summary>
        Union,

        /// <summary>
        /// The derived structure is the set difference of the two arguments, LeftSide except RightSide.
        /// </summary>
        Except
    }

    /// <summary>
    /// Contains information about how a derived structure should be computed.
    /// </summary>
    public class DerivedStructure
    {
        /// <summary>
        /// The structure that is on the left side of the operator.
        /// </summary>
        public string LeftSide { get; set; }

        /// <summary>
        /// The structure that is on the right side of the operator.
        /// </summary>
        public string RightSide { get; set; }

        /// <summary>
        /// The operator that should be applied to combine left and right side.
        /// </summary>
        public DerivedStructureOperator Operator { get; set; }

        /// <summary>
        /// The name that the resulting binary mask should have.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Creates a human-readable description of the object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var op =
                Operator == DerivedStructureOperator.Union
                ? "+"
                : Operator == DerivedStructureOperator.Except
                ? "\\"
                : Operator.ToString();
            return $"{Result} = {LeftSide} {op} {RightSide}";
        }
    }

    [Verb("dataset", HelpText = "Converts a DICOM dataset to Nifti.")]
    public class CommandlineCreateDataset : CommandlineShared
    {
        /// <summary>
        /// A regular expression that will be used wherever a structure name should be matched.
        /// Examples of matched strings: "ab", "a b", "some_thing", "some thing", "some thing "
        /// The matched string should be trimmed before further use.
        /// </summary>
        public const string NameRegex = @"[a-z0-9_][\.a-z0-9_\s]*";

        /// <summary>
        /// The separator character for all commandline options that are lists.
        /// </summary>
        public const char DefaultSeparator = ';';

        /// <summary>
        /// Full path to root directory for all datasets.
        /// </summary>
        [Option("datasetRootDirectory", Required = true, HelpText = "The root directory for all datasets.")]
        public string DatasetRootDirectory { get; set; } = string.Empty;

        /// <summary>
        /// The name of the DICOM dataset under <see cref="CommandlineShared.DatasetRootDirectory"/> to be converted to Nifti.
        /// </summary>
        [Option("dicomDatasetDirectory", Required = true, HelpText = "The name of the folder that contains the DICOM dataset within the root directory.")]
        public string DicomDirectory { get; set; }

        /// <summary>
        /// The name of the NIFTI dataset that will be created under <see cref="CommandlineShared.DatasetRootDirectory"/>.
        /// </summary>
        [Option("niftiDatasetDirectory", Required = true, HelpText = "The name of the folder that will contain the new nitfi dataset within the root directory.")]
        public string NiftiDirectory { get; set; }

        /// <summary>
        /// The priority mapping for structures, to ensure they are mutually exclusive. Only structures with names in this set will be included in the
        /// resulting dataset. Special case: if names are prefixed by "+", structures with those names will be included, but mutual exclusion will not be
        /// enforced between those structures and any others.
        /// </summary>
        [Option("groundTruthDescendingPriority", Separator = DefaultSeparator, HelpText = "Priority mappings for enforcing mutual exclusion for overlapping GT labels. In descending order (First highest priority). eg: Liver;Lung;Kidney_L;Kidney_R")]
        public IEnumerable<string> GroundTruthDescendingPriority { get; set; }

        /// <summary>
        /// If non-negative, the index of an element of PrespecifiedGroundTruthDescendingPriorities, which is then used instead of
        /// any value of --groundTruthDescendingPriority.
        /// </summary>
        [Option("priorityIndex", HelpText = "Index into a prespecified value for GroundTruthDescendingPriority")]
        public int GroundTruthDescendingPriorityIndex { get; set; } = -1;

        /// <summary>
        /// The names of the structures that should be created if they are missing from the dataset.
        /// </summary>
        [Option("createIfMissing", Separator = DefaultSeparator, HelpText = "The names of the structures, comma separated, that should be created if they are missing from the dicom dataset. This is done after structure renaming. For example: 'nec_cav;edema'")]
        public IEnumerable<string> CreateIfMissing { get; set; }

        /// <summary>
        /// A list of structure name mappings that should be applied to the dataset before doing any other operations on the dataset. 
        /// Example: 'A,B_something:C' means that all structures called A or B_something in the dicom dataset should be called C 
        /// in the nifti dataset.
        ///   If there are multiple structures that would map to the same name, the first one is preferred. For example, if the series
        /// contains structures "Adam" and "Eve", and renaming is run with the mapping "adam,eve:charly", the input structure
        /// "Adam" would be renamed to "charly". "Eve" will be left untouched, apart from lowercasing to "eve".
        ///   If the right hand side of the expression (after the colon) is prefixed by "+", then an augmentation rather than a
        /// renaming takes place: if "C" already exists, the voxels in each left-hand-side structure are added to C rather than
        /// replacing it (and the left-hand-side structures are kept).
        ///   On the left hand side, structures may be specified as "A.op.B", where "op" is one of "gt,ge,lt,le,intersection,union,minus".
        /// In this case, the source for the renaming (or augmentation) is computed from A and B if they both exist. The comparison
        /// operators refer to the vertical (z) dimension, so "A.gt.B" means "all voxels in A whose z value is greater than that of
        /// any voxel in B".
        ///   In the case of a renaming (expression ends with ":C"), if one left-hand-side element is successful, later ones are
        /// not tried; in the case of an augmentation, all elements are tried, and the augmentations are cumulative.
        ///   In general, you will want to have all augmentations listed after all renamings, so that you know what the structure
        /// names will be by the time you try the augmentations.
        /// </summary>
        [Option("rename", Separator = DefaultSeparator, HelpText = "A mapping of structure names in the Dicom dataset to structure names in the nifti dataset. All structure names will be converted to lower case. Format example: 'whole brain,whole_brain:brain;subtract:nec_cav'")]
        public IEnumerable<string> StructureNameMapping { get; set; }

        /// <summary>
        /// If non-negative, the index of an element of PrespecifiedNameMappings, which is then used instead of any value of --rename.
        /// </summary>
        [Option("renameIndex", HelpText = "Index for a prespecified renaming specification")]
        public int StructureNameMappingIndex { get; set; } = -1;

        /// <summary>
        /// If true, structure renaming causes a choice to be made between named structures when several options exist, rather than causing
        /// renaming to fail. Specifically, for a name mapping "oldName1,oldName2,...:newName":
        /// * If AllowNameClashes is false and structures exist for more than one of the names (old and/or new), renaming fails.
        /// * Otherwise, if a structure named newName exists, no renaming is done.
        /// * Otherwise, if a structure named oldNameK exists and no structures with names oldName1, ..., oldName[K-1] exist, then it is
        /// renamed to newName.
        /// Thus in summary, if names clash, an existing "newName" structure is preferred (we assume the oldNames are less-preferred variants
        /// of the canonical newName), otherwise earlier "oldName"s in the list have priority over later ones.
        /// </summary>
        [Option("allowNameClashes", Default = false, HelpText = "Whether to allow renaming when multiple structures with listed names exist")]
        public bool AllowNameClashes { get; set; } = false;

        /// <summary>
        /// Drop any structures with names containing the specified substring before name mappings are applied. This will usually have the
        /// effect (depending on other switches) of images containing such structures being discarded.
        /// </summary>
        [Option("dropNamesContaining", Default = null, HelpText = "Whether to drop any structures whose names contain the specified substring")]
        public string DropNamesContaining { get; set; } = null;

        /// <summary>
        /// Contains the information about all structure re-naming operations that should be performed at the beginning
        /// of the dataset preparation.
        /// </summary>
        public List<NameMapping> NameMappings { get; set; }

        [Option("registerOn", HelpText = "The reference channel to use during dataset creation when registering channels is necessary. Channel names will get a suffix '_onto_reference'. If not provided, no registration will be done.")]
        public string RegisterVolumesOnReferenceChannel { get; set; } = string.Empty;

        /// <summary>
        /// Use these spacings to run geometric normalization on the dataset. The spacings are given in millimeters.
        /// Using a spacing of 0 in any dimension means to not change this specific dimension.
        /// </summary>
        [Option("geoNorm", Separator = DefaultSeparator, HelpText = "The spacings to use for geometric normalization, in millimeters. 3 values must be provided, semicolon separated.")]
        public IEnumerable<double> GeometricNormalizationSpacingMillimeters { get; set; }
        
        /// <summary>
        /// Whether to discard (and report) invalid subjects and create a dataset from the rest (if there are
        /// any valid ones), as opposed to throwing an exception and not creating a dataset. If you set this switch,
        /// you should check the output of the job for information about discarded subjects.
        /// </summary>
        [Option("discardInvalidSubjects", Default = false, HelpText = "Whether to discard any subjects with problematic structure names, rather than failing the whole run")]
        public bool DiscardInvalidSubjects { get; set; }

        /// <summary>
        /// If this is set and GroundTruthDescendingPriority is defined, and all structures named in it must be present. If any are not,
        /// then either that volume is discarded (if DiscardInvalidSubjects is set) or an exception is raised.
        /// </summary>
        [Option("requireAllGroundTruthStructures", Default = false,
            HelpText = "Whether to fail (or drop the subject, if DiscardInvalidSubjects is set) if a subject does not have all the structures listed in GroundTruthDescendingPriority")]
        public bool RequireAllGroundTruthStructures { get; set; }

        /// <summary>
        /// Contains information about how derived structures should be added to the dataset.
        /// </summary>
        public IEnumerable<DerivedStructure> DerivedStructures { get; private set; }

        /// <summary>
        /// Gets or sets the un-processed commandline arguments that have been passed into the CreateDataset runner.
        /// </summary>
        public string[] RawCommandlineArguments { get; set; }

        /// <summary>
        /// Creates a new command line option instance, with all properties set to their default values.
        /// </summary>
        public CommandlineCreateDataset() { }

        /// <summary>
        /// Checks if the command line options are valid, and if so, sets certain values. Throws exceptions if any issues are found.
        /// </summary>
        override public void Validate()
        {
            if (StructureNameMappingIndex >= 0)
            {
                NameMappings = CommandlineCreateDatasetRecipes.PrespecifiedNameMappings[StructureNameMappingIndex].ToList();
                Trace.TraceInformation($"Applying --renameIndex {StructureNameMappingIndex} gives NameMappings of length {NameMappings.Count}");
            }
            else
            {
                try
                {
                    NameMappings = StructureNameMapping?.Select(ParseNameMapping)?.ToList();
                    if (NameMappings != null)
                    {
                        Trace.TraceInformation($"Parsed value of --rename to NameMappings of length {NameMappings.Count}");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Unable to parse name mapping given in the --rename option.", ex);
                }
            }

            if (DropNamesContaining != null && NameMappings != null) {
                foreach (var nameMapping in NameMappings)
                {
                    nameMapping.DropOldNamesContaining(DropNamesContaining);
                }
            }

            if (GroundTruthDescendingPriorityIndex >= 0)
            {
                GroundTruthDescendingPriority = CommandlineCreateDatasetRecipes.PrespecifiedGroundTruthDescendingPriorities[GroundTruthDescendingPriorityIndex].ToList();
            }

            var geoNorm = GeometricNormalizationSpacingMillimeters?.Count() ?? 0;
            if (geoNorm != 0 && geoNorm != 3)
            {
                throw new InvalidOperationException("When using geometric normalization, exactly 3 values must be provided.");
            }
        }

        /// <summary>
        /// Parses a string that contains a name re-mapping, for example: A,B_something:C
        /// This means that all structures called A or B_something in the dicom dataset should be called C 
        /// in the nifti dataset.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static NameMapping ParseNameMapping(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("The mapping must be a non-empty string.");
            }

            text = text.ToLowerInvariant();
            var regex = new Regex($"({NameRegex}(\\s*,\\s*{NameRegex})*)\\s*:(\\+)?\\s*({NameRegex})");
            var oldNameRegex = new Regex(NameRegex);
            var match = regex.Match(text.Trim());
            if (match.Success)
            {
                var newName = match.Groups[4].Value;
                var allOldNames = match.Groups[1].Value;
                var oldNames = new HashSet<string>();
                foreach (Match oldNameMatch in oldNameRegex.Matches(allOldNames.Trim()))
                {
                    oldNames.Add(oldNameMatch.Value.Trim());
                }
                var isAugmentation = match.Groups[3].Value == "+";
                return new NameMapping(newName, oldNames.ToList(), isAugmentation);
            }
            else
            {
                throw new FormatException($"Provided mapping string '{text}' must be in the form oldName1,oldName2:newName");
            }
        }

        /// <summary>
        /// Parses a structure-priority mapping, like "A:0". In the result, the structure name will be converted to lower case.
        /// </summary>
        /// <returns></returns>
        public static (string Name, int Priority)? ParseStructurePriority(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            text = text.ToLowerInvariant();
            var regex = new Regex($"(^{NameRegex})(\\s*:\\s*)([-+]?\\d+)");
            var match = regex.Match(text.Trim());
            if (match.Success)
            {
                var structureName = match.Groups[1].Value.Trim();
                var priority = int.Parse(match.Groups[3].Value.Trim());
                return (structureName, priority);
            }
            else
            {
                throw new FormatException($"Provided priority string '{text}' must be in the form structurename:priority (eg: a:0,b:1,c_d:2,c_e:2)");
            }
        }

        /// <summary>
        /// Parses a structure-priority mapping list. Valid strings are A:0,B:1,C:2,D:2.
        /// Returns an array of arrays. First level of indexing is the priority (index 0 having
        /// the highest priority), second level are all structures which have that priority.
        /// </summary>
        /// <returns></returns>
        public static string[][] ParseStructurePriority(IEnumerable<string> mappings)
        {
            var gtPriority = new Dictionary<string, int>();
            foreach (var mapping in mappings ?? Enumerable.Empty<string>())
            {
                var nameAndPriority = ParseStructurePriority(mapping);
                if (nameAndPriority.HasValue)
                {
                    var (name, priority) = nameAndPriority.Value;
                    gtPriority.Add(name, priority);
                    Trace.TraceInformation($"GT structure {name} is set to priority {priority}");
                }
            }
            return
                gtPriority
                .GroupBy(x => x.Value)
                .OrderBy(x => x.Key)
                .Select(x => x.Select(y => y.Key).ToArray())
                .ToArray();
        }

        /// <summary>
        /// Parses a specification string for a derived structure. Valid strings look like
        /// 'result=leftSide-rightSide' or 'result=leftSide+rightSide'.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static DerivedStructure ParseDerivedStructure(string text)
        {
            text = text.ToLowerInvariant();
            var regex = new Regex($"({NameRegex})=\\s*({NameRegex})([+-])\\s*({NameRegex})");
            var match = regex.Match(text.Trim());
            if (match.Success)
            {
                var result = match.Groups[1].Value.Trim();
                var left = match.Groups[2].Value.Trim();
                var right = match.Groups[4].Value.Trim();
                var op = match.Groups[3].Value;
                return new DerivedStructure
                {
                    Result = result,
                    LeftSide = left,
                    RightSide = right,
                    Operator =
                        op == "+"
                        ? DerivedStructureOperator.Union
                        : DerivedStructureOperator.Except,
                };
            }
            else
            {
                throw new FormatException($"Provided derived structure string '{text}' must be in the form 'result=leftSide-rightSide'");
            }
        }

        /// <summary>
        /// Extracts all of the <see cref="RawCommandlineArgument"/> entries that are specific to dataset creation, and returns them
        /// as a single string. This will exclude all secrets and app keys, which are defined in the base class <see cref="CommandlineShared"/>
        /// </summary>
        /// <returns></returns>
        public string GetCommandlineArgsForReport()
        {
            if (RawCommandlineArguments == null)
            {
                return string.Empty;
            }

            var optionLongNames =
                typeof(CommandlineCreateDataset)
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .Select(p => p.GetCustomAttribute<OptionAttribute>())
                .Where(option => option != null)
                .Select(option => option.LongName)
                .ToArray();
            var result = new List<string>();
            for (var index = 0; index < RawCommandlineArguments.Length; index++)
            {
                var arg = RawCommandlineArguments[index];
                if (optionLongNames.Any(option => arg == $"--{option}") && index < RawCommandlineArguments.Length - 1)
                {
                    result.Add(arg);
                    var nextArg = RawCommandlineArguments[index + 1];
                    if (!nextArg.StartsWith("-"))
                    {
                        result.Add(nextArg);
                    }
                    index++;
                }
            }

            return string.Join(" ", result);
        }

        /// <summary>
        /// Creates a human readable string that summarizes all important settings that will influence
        /// dataset creation.
        /// </summary>
        /// <returns></returns>
        public string SettingsOverview()
        {
            var status = new StringBuilder();
            string Join<T>(IEnumerable<T> parts)
                => parts == null ? "" : string.Join(DefaultSeparator.ToString(), parts);
            status.AppendLine($"Commandline arguments: {GetCommandlineArgsForReport()}");
            status.AppendLine($"DICOM dataset name: {DicomDirectory}");
            status.AppendLine($"NIFTI dataset name: {NiftiDirectory}");
            status.AppendLine($"Reference channel for registration: {RegisterVolumesOnReferenceChannel}");
            status.AppendLine($"Structure priorities: {Join(GroundTruthDescendingPriority)}");
            status.AppendLine($"Structure renaming: {Join(StructureNameMapping)}");
            status.AppendLine($"Structures that are created if missing in the Dicom dataset: {Join(CreateIfMissing)}");
            status.AppendLine($"Geometric normalization: {Join(GeometricNormalizationSpacingMillimeters)}");
            status.AppendLine("Structure priorities unrolled:");
            if (GroundTruthDescendingPriority == null || !GroundTruthDescendingPriority.Any())
            {
                status.AppendLine("None given.");
            }
            else
            {
                GroundTruthDescendingPriority
                .ForEach((structures, index) => status.AppendLine($"Priority {index}: {string.Join(", ", structures)}"));
            }
            status.AppendLine($"Structure renaming unrolled:");
            if (NameMappings.IsNullOrEmpty())
            {
                status.AppendLine("None given.");
            }
            else
            {
                NameMappings
                .ForEach(mapping => mapping.OldNames.ForEach(old => status.AppendLine($"'{old}' -> '{mapping.NewName}'")));
            }
            return status.ToString();
        }

        /// <summary>
        /// Writes a model to a string in JSON format.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns></returns>
        public static string ObjectToJson(object obj)
        {
            var settings =
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                };
            return JsonConvert.SerializeObject(obj,
                Formatting.Indented,
                settings);
        }
    }
}
