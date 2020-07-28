///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Readers
{
    using Dicom;
    using MedLib.IO.RT;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This class represents DICOM IOD modules associated with a volumetric representation of a DICOM series. 
    /// TODO: DicomSeriesReader repeats much of the activity here - that class should generate this information.
    /// TODO: The DICOM entities {Study,Patient,Series, FrameOfReference, Equipment} will be the same across 
    /// a set of images related to a volume. Change this class so it is a collection of Images relating to the same
    /// series and make this knowledge explicit by sharing the higher level instances. 
    /// </summary>
    public class DicomIdentifiers
    {
        /// <summary>
        /// Study level information 
        /// </summary>
        public DicomStudy Study { get; }

        /// <summary>
        /// Patient level information
        /// </summary>
        public DicomPatient Patient { get; }

        /// <summary>
        /// Series level information
        /// </summary>
        public DicomSeries Series { get; }

        /// <summary>
        /// FrameOfReference info (Series Level) for this instance.
        /// </summary>
        public DicomFrameOfReference FrameOfReference { get; }

        /// <summary>
        /// Equipment info (Series Level) for this instance.
        /// </summary>
        public DicomEquipment Equipment { get; }

        /// <summary>
        /// Common CT/MR image information
        /// </summary>
        public DicomCommonImage Image { get; }
        
        /// <summary>
        /// All of the DICOM tags that can be converted to string in a big list. 
        /// </summary>
        public IReadOnlyList<(string Tag, string Value)> AllAsString { get; }

        public DicomIdentifiers(
            DicomPatient patient,
            DicomStudy study,
            DicomSeries series,
            DicomFrameOfReference frameOfReference,
            DicomEquipment equipment,
            DicomCommonImage image,
            IReadOnlyList<(string Tag, string Value)> allAsString
            )
        {
            Patient = patient;
            Study = study;
            Series = series;
            FrameOfReference = frameOfReference;
            Equipment = equipment;
            Image = image;
            AllAsString = allAsString;
        }

        /// <summary>
        /// Construct a DicomIdentifiers instance from the given dataset - will throw
        /// if none of the Type 1 tags could be located for the required IOD modules. 
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static DicomIdentifiers ReadDicomIdentifiers(DicomDataset ds)
        {
            var patient = DicomPatient.Read(ds);
            var study = DicomStudy.Read(ds);
            var series = DicomSeries.Read(ds);
            var frameOfReference = DicomFrameOfReference.Read(ds);
            var equipment = DicomEquipment.Read(ds);
            var image = DicomCommonImage.Read(ds);

            // Extract all top level tags that can be converted to string. 
            var allAsString = ds
                .Where(x => x.ValueRepresentation.IsString)
                .Select((DicomItem x) => (Tag: x.Tag.DictionaryEntry.Name, Value: ds.GetSingleValueOrDefault(x.Tag, string.Empty)))
                .ToList();
            return new DicomIdentifiers(
                patient,
                study,
                series,
                frameOfReference,
                equipment,
                image,
                allAsString);
        }

        /// <summary>
        /// Constuct an empty DicomIdentifiers instance. 
        /// </summary>
        /// <returns></returns>
        public static DicomIdentifiers CreateEmpty()
        {
            return new DicomIdentifiers(
                DicomPatient.CreateEmpty(),
                DicomStudy.CreateEmpty(),
                DicomSeries.CreateEmpty(),
                DicomFrameOfReference.CreateEmpty(),
                DicomEquipment.CreateEmpty(),
                DicomCommonImage.CreateEmpty(),
                new List<(string Tag, string Value)> ()
                );
        }
    }
}
