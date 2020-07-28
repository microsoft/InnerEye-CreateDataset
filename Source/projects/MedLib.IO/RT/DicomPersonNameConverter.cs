///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.RT
{
    using System.Globalization;
    using Dicom;

    public class DicomPersonNameConverter
    {
        public DicomPersonNameConverter(string firstName, string lastName, string middleName, string prefix, string suffix)
        {
            First = firstName;
            Last = lastName;
            Middle = middleName;
            Prefix = prefix;
            Suffix = suffix;
        }

        public DicomPersonNameConverter(string dicomFormattedPatientName)
        {
            if (string.IsNullOrWhiteSpace(dicomFormattedPatientName))
            {
                return;
            }

            var elements = dicomFormattedPatientName.Split('^');

            if (elements.Length > 0)
            {
                Last = elements[0];
            }

            if (elements.Length > 1)
            {
                First = elements[1];
            }

            if (elements.Length > 2)
            {
                Middle = elements[2];
            }

            if (elements.Length > 3)
            {
                Prefix = elements[3];
            }

            if (elements.Length > 4)
            {
                Suffix = elements[4];
            }
        }

        public string First { get; set; }

        public string Last { get; set; }

        public string Middle { get; set; }

        public string Prefix { get; set; }

        public string Suffix { get; set; }

        public static string GetFormattedName(string dicomFormattedPatientName)
        {
            if (string.IsNullOrWhiteSpace(dicomFormattedPatientName))
            {
                return dicomFormattedPatientName;
            }

            return new DicomPersonNameConverter(dicomFormattedPatientName).FormattedName;
        }

        public string FormattedName
            =>
                $"{ToTitleCase(Prefix)}{(string.IsNullOrWhiteSpace(Prefix) ? "" : " ")}{ToTitleCase(First)}{(string.IsNullOrWhiteSpace(First) ? "" : " ")}{ToTitleCase(Middle)}{(string.IsNullOrWhiteSpace(Middle) ? "" : " ")}{ToTitleCase(Last)}{(string.IsNullOrWhiteSpace(Last) ? "" : " ")}{Suffix}".Trim()
        ;

        public string ToTitleCase(string str)
        {
            return string.IsNullOrEmpty(str) ? str : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower(CultureInfo.CurrentCulture));
        }

        public DicomPersonName AsPersonName(DicomTag dicomTag)
        {
            return new DicomPersonName(dicomTag, Last, First, Middle, Prefix, Suffix);
        }
    }
}