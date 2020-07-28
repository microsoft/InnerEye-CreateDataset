module InnerEye.CreateDataset.Data.ActivePatterns

///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

open System

/// An active pattern to match strings that start with the given prefix string. Matching result
/// is the string with the prefix stripped off
let (|StartsWith|_|) startString (s: string) =
    if s.StartsWith startString then
        s.Substring startString.Length
        |> Some
    else
        None

/// An active pattern to match strings that end with the given postfix string. Matching result
/// is the string with the postfix stripped off
let (|EndsWith|_|) endString (s: string) =
    if s.EndsWith endString then
        s.Substring(0, s.Length - endString.Length)
        |> Some
    else
        None

/// An active pattern to recognize strings that are null or empty.
let (|IsNullOrEmpty|NonEmptyString|) (s: string) = 
    if String.IsNullOrEmpty s then
        IsNullOrEmpty s
    else
        NonEmptyString s

/// An active pattern to recognize strings that are null or whitespace.
let (|IsNullOrWhiteSpace|NonTrivialString|) (s: string) = 
    if String.IsNullOrWhiteSpace s then
        IsNullOrWhiteSpace s
    else
        NonTrivialString s

/// Splits a string at the last ccurrence of a given character. Returns the string
/// up to the character, and the string after the character. Returns None if the character
/// does not occur in the string.
let (|SplitByLastIndexOf|_|) (splitChar: char) (s: string) =
    match s.LastIndexOf splitChar with
    | index when index < 0 -> 
        None
    | index -> 
        Some(s.Substring(0, index), s.Substring(index + 1))

/// Splits a string at the first occurrence of a given character. Returns the string
/// up to the character. Returns None if the character does not occur in the string.
let (|UpToFirstIndexOf|_|) (splitChar: char) (s: string) =
    match s.IndexOf splitChar with
    | index when index < 0 -> 
        None
    | index -> 
        Some(s.Substring(0, index))

/// Splits a string at the first occurrence of a given character. Returns the string
/// starting after the character. Returns None if the character does not occur in the string.
let (|AfterFirstIndexOf|_|) (splitChar: char) (s: string) =
    match s.IndexOf splitChar with
    | index when index < 0 -> 
        None
    | index -> 
        Some(s.Substring(index + 1))

/// Splits a string at the first occurrence of a given string. Returns the string
/// up to the split, and the string after the split. Returns None if the splitting
/// string is not found.
let (|SplitByString|_|) (splitString: string) (s: string) =
    match s.IndexOf splitString with
    | index when index < 0 -> 
        None
    | index -> 
        Some(s.Substring(0, index), s.Substring(index + splitString.Length))


/// An active pattern to match values in a System.Nullable, or check for it being null.
let (|HasValue|IsNull|) (n: Nullable<_>) =
    if n.HasValue then
        HasValue n.Value
    else
        IsNull
        