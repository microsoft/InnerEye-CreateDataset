namespace InnerEye.CreateDataset.Data

///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

open System.Collections.Generic
open System
open System.IO
open InnerEye.CreateDataset.Data.ActivePatterns

module ChannelNameHandling =
    /// <summary>
    /// Converts a channel name template (like "\layer\{0}\out.nii.gz") to a subject-specific 
    /// file name, by calling string.Format on the template.
    /// </summary>
    /// <param name="channelNameOrTemplate">The channel name template</param>
    /// <param name="subjectId">The numeric identifier of the subject.</param>
    let ExpandTemplate (subjectId: int<SubjectId>) channelNameOrTemplate =
        String.Format(channelNameOrTemplate, subjectId)
    
    /// Extracts a channel filename from a channel file path template.
    /// This is a hacky process relying on conventions used in models (in Model.cs files).
    let FilenameFromChannel (channel: string) =
        match Path.GetFileName channel with
        | IsNullOrWhiteSpace _ -> invalidArg "channel" ""
        | s -> (s.Split '.').[0]


/// Stores the paths of the files to process for a given patient/subject. The files
/// are keyed by a string channel name.
type SubjectFiles private (subjectId: int<SubjectId>, files: seq<DatasetFile>) =
    let dict =
        files
        |> Seq.map (fun file -> file.ChannelId, file)
        |> Map.ofSeq
    
    /// Gets the numeric identifier for the subject
    member this.SubjectId = subjectId

    /// Gets the file associated with the given channel name. Throws a KeyNotFoundException
    /// if no such file is present.
    member this.Item with get channelId = 
                        try 
                            dict.[channelId]
                        with 
                        | :? KeyNotFoundException ->
                            sprintf "Channel '%s' was not found in the dictionary." channelId
                            |> KeyNotFoundException
                            |> raise

    /// Gets whether a file with the given channel ID is present in the object.
    member this.ContainsKey channelId = dict.ContainsKey channelId

    /// Gets the number of files (channels) that that present object stores.
    member this.Count = dict.Count

    /// <summary>
    /// Attempts to get a channel-specific file from the present object. If such a channel is
    /// known to the object, return Some. If no such channel is known, return None.
    /// </summary>
    /// <param name="channelId">The name of the channel to retrieve.</param>
    member private this.TryGetValue channelId = 
        if this.ContainsKey channelId then
            Some dict.[channelId]
        else
            None

    /// <summary>
    /// Creates a new subject files object from a list of files. The channel name for the lookup
    /// is read from the ChannelId field of the file. Throws an ArgumentException if there is more 
    /// than 1 file associated with a channelId
    /// </summary>
    /// <param name="subjectId">The numeric subject ID. All files passed in the second argument are expected
    /// to belong to this subject.</param>
    /// <param name="files">The files that should be associated with this subject.</param>
    static member Create (subjectId, files: seq<DatasetFile>) =
        let filesList = files |> Seq.toList
        filesList
        |> Seq.iter (fun file -> 
            if file.SubjectId <> subjectId then
                let message = sprintf "All files must have the given subjectId %i, but received a file belonging to %i" subjectId file.SubjectId
                ArgumentException(message, "files")
                |> raise
        )
        filesList
        |> Seq.countBy (fun file -> file.ChannelId)
        |> Seq.iter (fun (channelId, numFiles) ->
            if numFiles > 1 then
                ArgumentException(sprintf "There is more than 1 file associated with channel %s" channelId, "files")
                |> raise
        )
        SubjectFiles(subjectId, files)

    /// Creates a new subject file storage that contains no image files.
    static member Empty subjectId = SubjectFiles(subjectId, Seq.empty)

    /// <summary>
    /// Converts a channel name template (like "\layer\{0}\out.nii.gz") to a subject-specific 
    /// file name, by calling string.Format on the template.
    /// </summary>
    /// <param name="channelNameOrTemplate">The channel name template</param>
    /// <param name="subjectId">The numeric identifier of the subject.</param>
    static member CreateFileNameFromChannelTemplate(channelNameOrTemplate, subjectId: int<SubjectId>) =
        ChannelNameHandling.ExpandTemplate subjectId channelNameOrTemplate

    /// <summary>
    /// If a channel of the given name is known to the object, return its file information.
    /// This is an DatasetFile instance with IsRawImage = true.
    /// Otherwise, interpret the channel as a string template for a channel file 
    /// (like "\layer\{0}\out.nii.gz") and create the subject-specific file name,
    /// by replacing {0} with the subject ID. This is returned as an DatasetFile 
    /// with IsRawImage = false.
    /// </summary>
    /// <param name="channelName">The channel name to retrieve</param>
    member this.GetChannelFile channelName =
        match this.TryGetValue channelName with
        | Some file -> file
        | None ->
            let fullName = SubjectFiles.CreateFileNameFromChannelTemplate(channelName, this.SubjectId)
            DatasetFile.CreateIntermediate(subjectId, fullName, channelName)

    /// Gets that names of all channels that would be read from the raw subject data.
    member this.GetRawChannels() = 
        dict
        |> Seq.choose (function 
            | KeyValue(_, file) -> 
                if file.IsRawImage then
                    Some file.ChannelId
                else
                    None
        )

    /// Gets all files that are stored in the present object.
    member this.AllFiles() = dict |> Map.toSeq |> Seq.map snd
