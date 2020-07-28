///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using Dicom;
    using Extensions;

    public class DicomStudy
    {
        /// <summary>
        /// The unique identifier for this study, Type 1, VR UI
        /// </summary>
        public string StudyInstanceUid { get; }

        /// <summary>
        /// The date the study started, Type 2
        /// </summary>
        public string StudyDate { get; }

        /// <summary>
        /// The time the study started, Type 2
        /// </summary>
        public string StudyTime { get; }

        /// <summary>
        /// Name of the physician who referred the patient, Type 2, PN 
        /// </summary>
        public string ReferringPhysicianName { get; }

        /// <summary>
        /// User or equipment generated study identifier, Type 3 VR short string
        /// </summary>
        public string StudyId { get; }

        /// <summary>
        /// RIS generated number for study, Type 2, Short string
        /// </summary>
        public string AccessionNumber { get; }

        /// <summary>
        /// Description, Type 3, Long string
        /// </summary>
        public string StudyDescription { get; }

        public DicomStudy(string studyInstanceUid, string studyDate, string studyTime, string referringPhysicianName, string studyId, string accessionNumber, string studyDescription)
        {
            StudyInstanceUid = studyInstanceUid;
            StudyDate = studyDate;
            StudyTime = studyTime;
            ReferringPhysicianName = referringPhysicianName;
            StudyId = studyId;
            AccessionNumber = accessionNumber;
            StudyDescription = studyDescription;
        }

        /// <summary>
        /// Creates an empty study instance.
        /// </summary>
        /// <returns></returns>
        public static DicomStudy CreateEmpty()
        {
            var instanceUid = DicomExtensions.EmptyUid.UID;
            var studyDate = string.Empty;
            var studyTime = string.Empty;
            var physicianName = string.Empty;
            var studyId = string.Empty;
            var accessionNumber = string.Empty;
            var studyDescription = string.Empty;

            return new DicomStudy(
                instanceUid,
                studyDate,
                studyTime,
                physicianName,
                studyId,
                accessionNumber,
                studyDescription);
        }

        public static DicomStudy Read(DicomDataset ds)
        {
            // throw
            var instanceUid = ds.GetSingleValue<DicomUID>(DicomTag.StudyInstanceUID).UID;

            // no throw
            var studyDate = ds.GetStringOrEmpty(DicomTag.StudyDate);
            var studyTime = ds.GetStringOrEmpty(DicomTag.StudyTime);
            var physicianName = ds.GetStringOrEmpty(DicomTag.ReferringPhysicianName);
            var studyId = ds.GetTrimmedStringOrEmpty(DicomTag.StudyID);
            var accessionNumber = ds.GetTrimmedStringOrEmpty(DicomTag.AccessionNumber);
            var studyDescription = ds.GetTrimmedStringOrEmpty(DicomTag.StudyDescription);

            return new DicomStudy(
                instanceUid,
                studyDate,
                studyTime,
                physicianName,
                studyId,
                accessionNumber,
                studyDescription);
        }

        public static void Write(DicomDataset ds, DicomStudy study)
        {
            ds.Add(DicomTag.StudyInstanceUID, study.StudyInstanceUid);
            ds.Add(DicomTag.StudyDate, study.StudyDate);
            ds.Add(DicomTag.StudyTime, study.StudyTime);
            ds.Add(DicomTag.ReferringPhysicianName, study.ReferringPhysicianName);
            ds.Add(DicomTag.StudyID, study.StudyId);
            ds.Add(DicomTag.AccessionNumber, study.AccessionNumber);
            ds.Add(DicomTag.StudyDescription, study.StudyDescription);
        }
    }
}