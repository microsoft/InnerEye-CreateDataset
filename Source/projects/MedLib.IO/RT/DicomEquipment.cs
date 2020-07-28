///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using Dicom;
    using Extensions;

    /// <summary>
    /// Encodes part of the DICOM Equipment module
    /// http://dicom.nema.org/medical/Dicom/current/output/chtml/part03/sect_C.7.5.html
    /// </summary>
    public class DicomEquipment
    {
        /// <summary>
        /// Type 2, Long String (64 chars)
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Type 3 Long string 
        /// </summary>
        public string SoftwareVersions { get; set; }

        /// <summary>
        /// Device name
        /// </summary>
        public string Device { get; set; }

        private DicomEquipment(string manufacturer, string softwareVersions)
        {
            Manufacturer = manufacturer;
            SoftwareVersions = softwareVersions;

        }

        public DicomEquipment(string manufacturer, string device, string softwareVersions)
        {
            Manufacturer = manufacturer;
            Device = device;
            SoftwareVersions = softwareVersions;
        }

        public static DicomEquipment Read(DicomDataset ds)
        {
            var manufacturer = ds.GetTrimmedStringOrEmpty(DicomTag.Manufacturer);
            var softwareVersions = ds.GetTrimmedStringOrEmpty(DicomTag.SoftwareVersions);
            return new DicomEquipment(manufacturer, softwareVersions);
        }

        public static void Write(DicomDataset ds, DicomEquipment equipment)
        {
            ds.Add(DicomTag.Manufacturer, equipment.Manufacturer);
            ds.Add(DicomTag.SoftwareVersions, equipment.SoftwareVersions);
        }

        /// <summary>
        /// Creates an empty DicomEquipment instance
        /// </summary>
        /// <returns></returns>
        public static DicomEquipment CreateEmpty()
        {
            return new DicomEquipment(string.Empty, string.Empty);
        }
    }
}