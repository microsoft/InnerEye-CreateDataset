///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.RT
{
    using System;
    using System.Collections.Generic;

    using Dicom;

    using Extensions;

    public enum ROIInterpretedType
    {
        None,
        CTV,
        ORGAN,
        EXTERNAL
    }

    /// <summary>
    /// Elements we need from the RT ROI Observation Module:
    /// see http://dicom.nema.org/medical/Dicom/current/output/chtml/part03/sect_C.8.8.8.html
    /// </summary>
    public class DicomRTObservation
    {
        public string ReferencedRoiNumber { get; }

        public DicomPersonNameConverter RoiInterpreterName { get; }

        public ROIInterpretedType ROIInterpretedType { get; }

        public DicomRTObservation(string referencedRoiNumber,
            DicomPersonNameConverter roiInterpreterName, 
            ROIInterpretedType roiInterpretedType = ROIInterpretedType.None)
        {
            ReferencedRoiNumber = referencedRoiNumber;
            RoiInterpreterName = roiInterpreterName;
            ROIInterpretedType = roiInterpretedType;

        }

        /// <summary>
        /// Returns a list of observations found in the given data set. 
        /// </summary>
        /// <remarks>
        /// The standard does not impose any relationship between the number of observations and the
        /// referenceROI there can be 0 or more. 
        /// </remarks>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static IReadOnlyList<DicomRTObservation> Read(DicomDataset ds)
        {
            var observations = new List<DicomRTObservation>(); 
        
            if (ds.Contains(DicomTag.RTROIObservationsSequence))
            {
                var seq = ds.GetSequence(DicomTag.RTROIObservationsSequence);

                foreach (var item in seq)
                {
                    var roiNumber = item.GetStringOrEmpty(DicomTag.ReferencedROINumber);
                    var roiInterpreter = item.GetStringOrEmpty(DicomTag.ROIInterpreter);
                    var roiInterpretedTypeStr = item.GetStringOrEmpty(DicomTag.RTROIInterpretedType);
                    ROIInterpretedType roiInterpretedType;
                    
                    if (Enum.IsDefined(typeof(ROIInterpretedType), roiInterpretedTypeStr))
                    {
                        Enum.TryParse(roiInterpretedTypeStr, true, out roiInterpretedType);
                    }   
                    else
                    {
                        roiInterpretedType = ROIInterpretedType.None;
                    }
                    observations.Add(new DicomRTObservation(roiNumber, new DicomPersonNameConverter(roiInterpreter), roiInterpretedType));
                }
            }
            return observations;
        }

        public static void Write(DicomDataset ds, IEnumerable<DicomRTObservation> observations)
        {
            var listOfObservations = new List<DicomDataset>();
            var observationNumber = 0; 

            foreach (var obs in observations)
            {
                var roiInterpretedType = obs.ROIInterpretedType == ROIInterpretedType.None ? string.Empty : obs.ROIInterpretedType.ToString();
                var newDs = new DicomDataset
                {
                    // Type 1 required tag - only needs to be unique within this sequence, not referenced externally. 
                    {DicomTag.ObservationNumber, observationNumber++},
                    {DicomTag.ReferencedROINumber, obs.ReferencedRoiNumber},
                    {DicomTag.RTROIInterpretedType, roiInterpretedType},
                    {DicomTag.ROIInterpreter, obs.RoiInterpreterName.AsPersonName(DicomTag.ROIInterpreter).Get<string>()}
                };
                listOfObservations.Add(newDs);
            }

            ds.Add(new DicomSequence(DicomTag.RTROIObservationsSequence, listOfObservations.ToArray()));
        }
    }
}