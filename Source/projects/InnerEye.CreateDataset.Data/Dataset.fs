namespace InnerEye.CreateDataset.Data

///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

open System.Collections.Generic
open System.IO

/// Stores a set of per-subject files. The first index is via a numeric subject ID,
/// inside is lookup from a string channel name to an actual file.
type Dataset private (dataset: Map<int<SubjectId>, SubjectFiles>) =

    /// Gets the subject files associated with the given subject. Throws a KeyNotFoundException
    /// if no such files are present.
    member this.Item with get subjectId = dataset.[subjectId]

    /// Gets whether subject files for the given subjectId are present in the object.
    member this.ContainsKey subjectId = dataset.ContainsKey subjectId

    /// Gets the number of subjects that that present object stores.
    member this.Count = dataset.Count

    /// Gets the set of subject IDs that the present object stores,
    /// sorted ascendingly.
    member this.GetSubjects() =
        dataset
        |> Seq.map (function KeyValue(subject, _) -> subject)
        |> Seq.sort

    /// Creates a new dataset lookup from a set of file descriptions.
    static member Create (files: seq<DatasetFile>) =
        files
        |> Seq.groupBy (fun file -> file.SubjectId)
        |> Seq.map (fun (subjectId, subjectFiles) ->
            subjectId, SubjectFiles.Create(subjectId, subjectFiles)
        )
        |> Map.ofSeq
        |> Dataset

    /// Creates a new dataset that contains only of those subjects with subject IDs
    /// that are in the present object and in the argument.
    member this.RestrictTo (subjects: seq<int<SubjectId>>) =
        let subjectsToRetain = Set.ofSeq subjects
        dataset
        |> Map.filter (fun subjectId _ -> subjectsToRetain.Contains subjectId)
        |> Dataset

    /// Creates an empty dataset, without any subjects.
    static member Empty = Dataset Map.empty

    /// Gets the names of all raw data channels that are present for all subjects 
    /// in the dataset. For example, if subject1 has raw data channels "ct" and "cta",
    /// but subject2 has only raw data channel "ct", the method returns "ct".
    member this.GetRawChannels() = 
        if dataset.Count > 0 then
            dataset
            |> Seq.map (function 
                | KeyValue(_, files) -> files.GetRawChannels() |> Set.ofSeq)
            |> Set.intersectMany
            |> Set.toSeq
        else
            Seq.empty

    /// Verifies that all raw data has the same channel names across all subjects.
    /// Returns an empty sequence if the dataset is wellformed. Returns a non-empty
    /// sequence of human readable error messages if any inconsistencies are found.
    member this.Validate() = 
        let printRawChannels channels = 
            let all = channels |> String.concat "; "
            "'" + all + "'"
        match dataset |> Map.toList with
        | [] -> Seq.empty
        | (subject1, subject1Files) :: others ->
            /// The raw channels that are available for the first subject in the dataset
            let raw1 = subject1Files.GetRawChannels() |> Seq.toList
            let raw1String = raw1 |> printRawChannels
            others
            |> Seq.choose (fun (subject, files) ->
                let channels = files.GetRawChannels() |> Seq.toList
                if channels <> raw1 then
                    sprintf "Subject %i has raw channels %s, but subject %i has raw channels %s"
                        subject1 raw1String subject (printRawChannels channels)
                    |> Some
                else
                    None
            )
    
    /// Checks that the model inputs are present in all subjects of the dataset
    member this.ValidateWithInputs(layerInputs:string seq) =
        let layerInputSet = layerInputs |> Set.ofSeq 
        let printRawChannels channels = 
            let all = channels |> String.concat "; "
            "'" + all + "'"
        match dataset |> Map.toList with
        | [] -> Seq.empty
        | (_, _) :: others ->
            /// The raw channels that are available for the first subject in the dataset
            let modelChannelsString = layerInputSet |> printRawChannels
            others
            |> Seq.choose (fun (subject, files) ->
                let channels = files.GetRawChannels() |> Set.ofSeq
                // Check that channels contain all the model inputs
                if not(layerInputSet.IsSubsetOf channels) then
                    let missing = Set.difference layerInputSet channels
                    sprintf "Subject %i does not contain the required channel(s) %s. Model has input channels %s, but data for subject %i contains channels %s"
                        subject 
                        (printRawChannels missing)
                        modelChannelsString
                        subject 
                        (printRawChannels channels)
                    |> Some
                else
                    None
            )
        

    interface IEnumerable<KeyValuePair<int<SubjectId>, SubjectFiles>> with
        member this.GetEnumerator() = 
            (Seq.cast<KeyValuePair<int<SubjectId>, SubjectFiles>> dataset).GetEnumerator() 
        member this.GetEnumerator() = 
            (Seq.cast dataset).GetEnumerator()
            :> System.Collections.IEnumerator

    
    /// Breaks text into individual lines, returned as a sequence. If the argument is null, the result is a sequence
    /// with a single entry that is null.
    static member TextToLines text = 
        if text = null then
            Seq.singleton null
        else
            let reader = new StringReader(text)
            let line = ref (reader.ReadLine())
            seq {
                while !line <> null do
                    yield !line
                    line := reader.ReadLine()
            }
