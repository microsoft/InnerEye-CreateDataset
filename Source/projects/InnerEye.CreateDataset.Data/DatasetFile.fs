namespace InnerEye.CreateDataset.Data

///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

open System.Diagnostics

[<Measure>]
type SubjectId

/// Stores information about a subject.
type SubjectData =
    {
        /// Gets the unique identifier of the subject's scan.
        SeriesId: string
        /// Gets the Institution id associated with the subject.
        InstitutionId: string
        /// Gets the path to the image data associated with the subject.
        ImageFilePath : string
        /// Gets the path to the ground truth data associated with the subject.
        GroundTruthFilePath : string
        /// Gets the tags associated with the subject.
        Tags: string seq
    }

/// Identifies whether a file is an intermediate output or a raw volume
type RawOrIntermediate =
    /// Indicates that the associated file is some output of the program.
    | IntermediateOutput
    /// Indicates that the associated file is raw ground truth.
    | Raw of subjectData: SubjectData

[<DebuggerDisplay("{DebuggerDisplay()}")>]
type DatasetFile =
    {
        /// Gets a numeric ID for the patient that this image belongs to.
        SubjectId : int<SubjectId>
        /// Gets the relative path where the file is stored, in a repository of intermediate or raw files.
        FilePath : string
        /// Gets the channel ID of the image.
        ChannelId : string
        /// Gets where the image was read from or produced.
        Producer: RawOrIntermediate
    }

    /// <summary>
    /// Gets whether the image is a raw image, coming directly out of the dataset. If false, it is an intermediate
    /// image that has been written by the program.
    /// </summary>
    member this.IsRawImage =
        match this.Producer with
        | IntermediateOutput -> false
        | Raw _ -> true

    /// Gets the subject-specific data, when the present object
    /// represents a volume coming from the dataset. Throw an InvalidOperationException
    /// if called on an image that is not directly from the dataset.
    member this.SubjectData() =
        match this.Producer with
        | IntermediateOutput -> invalidOp "This method can only be used for raw images."
        | Raw rawData -> rawData

    /// <summary>
    /// Creates an instance of the class, for files that are read directly from the dataset.
    /// </summary>
    /// <param name="subjectId"></param>
    /// <param name="filePath"></param>
    /// <param name="channelId"></param>
    /// <param name="seriesId"></param>
    /// <param name="institutionId"></param>
    /// <param name="imageFilePath"></param>
    /// <param name="groundTruthFilePath"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    static member CreateRaw(subjectId, filePath, channelId, seriesId, institutionId, imageFilePath, groundTruthFilePath, tags) =
        let tags = 
            match tags with
            | null -> []
            | tags -> tags |> Seq.toList
        let data = 
            {
                SeriesId = seriesId
                InstitutionId = institutionId
                ImageFilePath = imageFilePath
                GroundTruthFilePath = groundTruthFilePath
                Tags = tags
            }
        {
            SubjectId = subjectId
            FilePath = filePath
            ChannelId = channelId
            Producer = Raw data
        }

    /// <summary>
    /// Creates an instance for files that are intermediate outputs of the pipeline.
    /// </summary>
    /// <param name="subjectId"></param>
    /// <param name="filePath"></param>
    /// <param name="channelId"></param>
    /// <returns></returns>
    static member CreateIntermediate(subjectId, filePath, channelId) =
        {
            SubjectId = subjectId
            FilePath = filePath
            ChannelId = channelId
            Producer = IntermediateOutput
        }

    /// <summary>
    /// Creates an instance of the class for use in unit tests, with default values for SeriesId 
    /// and tags.
    /// </summary>
    /// <param name="subjectId"></param>
    /// <param name="filePath"></param>
    /// <param name="channelId"></param>
    /// <returns></returns>
    static member CreateRawForTesting(subjectId, filePath, channelId) =
        DatasetFile.CreateRaw(subjectId, filePath, channelId, subjectId.ToString(), System.String.Empty, System.String.Empty, System.String.Empty, [])

    /// <summary>
    /// Creates a short summary string of the object that will be displayed in the VS Watch window.
    /// </summary>
    /// <returns></returns>
    member this.DebuggerDisplay() =
        (if this.IsRawImage then "Raw" else "Intermediate") + " " + this.FilePath
