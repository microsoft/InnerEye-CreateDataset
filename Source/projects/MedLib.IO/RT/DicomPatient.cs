///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using Dicom;

    using Extensions;

    /// <summary>
    /// Encodes part of the DICOM Patient Module
    /// see http://dicom.nema.org/medical/Dicom/current/output/chtml/part03/sect_C.7.html
    /// </summary>
    public class DicomPatient
    {
        public DicomPatient(DicomPersonNameConverter name, string id, string birthDate, string sex)
        {
            Name = name;
            Id = id;
            BirthDate = birthDate;
            Sex = sex;
        }

        /// <summary>
        /// Patient Name in PN format Type 2.
        /// </summary>
        public DicomPersonNameConverter Name { get; }

        /// <summary>
        /// Institution's PatientID Type 2
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Patient's birthdate DA format Type 2
        /// </summary>
        public string BirthDate { get; }

        /// <summary>
        /// Patient's gender code string {M,F,O} Type 2
        /// </summary>
        public string Sex { get; }

        public static void Write(DicomDataset ds, DicomPatient patient)
        {
            ds.Add(DicomTag.PatientName, patient.Name.AsPersonName(DicomTag.PatientName).Get<string>());
            ds.Add(DicomTag.PatientID, patient.Id);
            ds.Add(DicomTag.PatientBirthDate, patient.BirthDate);
            ds.Add(DicomTag.PatientSex, patient.Sex);
        }

        public static DicomPatient Read(DicomDataset ds)
        {
            var patientName = ds.GetStringOrEmpty(DicomTag.PatientName);
            var patientId = ds.GetTrimmedStringOrEmpty(DicomTag.PatientID);
            var patientBirthDate = ds.GetStringOrEmpty(DicomTag.PatientBirthDate);
            var patientSex = ds.GetTrimmedStringOrEmpty(DicomTag.PatientSex);
            return new DicomPatient(new DicomPersonNameConverter(patientName), patientId, patientBirthDate, patientSex);
        }

        /// <summary>
        /// Return an empty DicomPatient instance
        /// </summary>
        /// <returns></returns>
        public static DicomPatient CreateEmpty()
        {
            return new DicomPatient(
                new DicomPersonNameConverter(string.Empty),
                string.Empty,
                string.Empty,
                string.Empty
            );
        }

    }
}