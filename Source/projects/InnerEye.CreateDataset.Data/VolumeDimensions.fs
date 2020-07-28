namespace InnerEye.CreateDataset.Data

///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

open System
open InnerEye.CreateDataset.Volumes
open System.Diagnostics

/// Describes the size of a 3D volume.
[<CLIMutableAttribute>]
type Volume3DDimensions =
    {
        /// The size of the volume in the X dimension
        X: int
        /// The size of the volume in the Y dimension
        Y: int
        /// The size of the volume in the Z dimension
        Z: int
    }

    override this.ToString() = sprintf "%i x %i x %i" this.X this.Y this.Z

    /// Creates a Volume3DDimensions instance from the arguments.
    static member Create (dimX, dimY, dimZ) = { X = dimX; Y = dimY; Z = dimZ }

    /// Creates a VolumeDimensions instance that stores the size of the given Volume3D instance.
    static member Create (volume: Volume3D<_>) = 
        { X = volume.DimX; Y = volume.DimY; Z = volume.DimZ }

    /// Returns true if the volume dimensions in the present object are strictly smaller in each dimension
    /// than the volume dimensions given in the argument.
    member this.IsStrictlySmallerThan (other: Volume3DDimensions) = 
        this.X < other.X 
        && this.Y < other.Y
        && this.Z < other.Z
   

/// Stores a point as an (x, y, z) tuple, with an equality operation that uses relative difference.
[<CLIMutableAttribute>]
type Tuple3D = 
    {
        /// The component in X dimension.
        X: double
        /// The component in Y dimension.
        Y: double
        /// The component in Z dimension.
        Z: double
    }

    override this.ToString() = 
        sprintf "X = %g; Y = %g; Z = %g" this.X this.Y this.Z

    /// <summary>
    /// Gets whether the point stored in the present object and the point in the argument
    /// should be considered equal, when looking at componentwise relative difference.
    /// The function returns true if, along all 3 dimensions, the pairwise relative
    /// difference is below the given threshold value. If any of the dimensions has a
    /// mismatch, detailed information is printed to Trace.
    /// </summary>
    /// <param name="other">The other tuple to which the present object should be compared.</param>
    /// <param name="maximumRelativeDifference">The maximum allowed relative difference along a dimension.</param>
    /// <param name="loggingPrefix">If a dimension has differences over the allowed maximum,
    /// print details to TraceWarning, with this string printed before the dimension.</param>
    member this.HasSmallRelativeDifference (other: Tuple3D, maximumRelativeDifference, loggingPrefix) =
        let equal dimension (x: double) (y: double) = 
            let diff =
                match x with
                | 0.0 -> Math.Abs y
                | nonZeroX -> Math.Abs(1.0 - y / nonZeroX)
            let isEqual = diff <= maximumRelativeDifference
            if not isEqual then 
                sprintf "Relative difference in %s%s is %f, but only %f is allowed" loggingPrefix dimension diff maximumRelativeDifference 
                |> Trace.TraceWarning
            isEqual
        equal "X" this.X other.X && equal "Y" this.Y other.Y && equal "Z" this.Z other.Z

    /// <summary>
    /// Gets whether the point stored in the present object and the point in the argument
    /// should be considered equal, when looking at componentwise relative difference.
    /// The function returns true if, along all 3 dimensions, the pairwise relative
    /// difference is below the given threshold value. If any of the dimensions has a
    /// mismatch, detailed information is printed to Trace.
    /// </summary>
    /// <param name="other">The other tuple to which the present object should be compared.</param>
    /// <param name="maximumRelativeDifference">The maximum allowed relative difference along a dimension.</param>
    member this.HasSmallRelativeDifference (other: Tuple3D, maximumRelativeDifference) =
        this.HasSmallRelativeDifference(other, maximumRelativeDifference, String.Empty)

    /// <summary>
    /// Gets whether the point stored in the present object and the point in the argument
    /// should be considered equal, when looking at componentwise abolute difference.
    /// The function returns true if, along all 3 dimensions, the pairwise absolute
    /// difference is below the given threshold value. If any of the dimensions has a
    /// mismatch, detailed information is printed to Trace.
    /// </summary>
    /// <param name="other">The other tuple to which the present object should be compared.</param>
    /// <param name="maximumAbsoluteDifference">The maximum allowed absolute difference along a dimension.</param>
    /// <param name="loggingPrefix">If a dimension has differences over the allowed maximum,
    /// print details to TraceWarning, with this string printed before the dimension.</param>
    member this.HasSmallAbsoluteDifference (other: Tuple3D, maximumAbsoluteDifference, loggingPrefix) =
        let equal dimension (x: double) (y: double) = 
            let diff = Math.Abs(x - y) 
            let isEqual = diff <= maximumAbsoluteDifference
            if not isEqual then 
                sprintf "Absolute difference in %s%s is %f, but only %f is allowed" loggingPrefix dimension diff maximumAbsoluteDifference 
                |> Trace.TraceWarning
            isEqual
        equal "X" this.X other.X && equal "Y" this.Y other.Y && equal "Z" this.Z other.Z

    /// <summary>
    /// Gets whether the point stored in the present object and the point in the argument
    /// should be considered equal, when looking at componentwise abolute difference.
    /// The function returns true if, along all 3 dimensions, the pairwise absolute
    /// difference is below the given threshold value. If any of the dimensions has a
    /// mismatch, detailed information is printed to Trace.
    /// </summary>
    /// <param name="other">The other tuple to which the present object should be compared.</param>
    /// <param name="maximumAbsoluteDifference">The maximum allowed absolute difference along a dimension.</param>
    member this.HasSmallAbsoluteDifference (other: Tuple3D, maximumAbsoluteDifference) =
        this.HasSmallAbsoluteDifference(other, maximumAbsoluteDifference, String.Empty)

    /// Converts the present object into an array of length 3, with the X, Y, Z components.
    member this.toArray() = [| this.X; this.Y; this.Z |]

    /// Creates an instance of Point3DWithSlack from an array of length 3.
    static member fromArray a =
        match a with
        | null -> nullArg "a"
        | [| x0; x1; x2 |] -> { X = x0; Y = x1; Z = x2 }
        | _ -> invalidArg "a" "size mismatch"
 
 /// 3D direction matrix for a Volume3D
[<CLIMutableAttribute>]
type Direction3D =
    {
        X0: Tuple3D
        X1: Tuple3D
        X2: Tuple3D
    }

    member this.toArray() = [this.X0.toArray(); this.X1.toArray(); this.X2.toArray()] |> Array.concat 
    
    static member fromArray a =
        match a with
        | null -> nullArg "a"
        | [| x0; x1; x2; x3; x4; x5; x6; x7; x8; |] -> { X0 = {X = x0; Y=x1; Z=x2}; X1 = {X = x3; Y=x4; Z=x5}; X2 = {X = x6; Y=x7; Z=x8} }
        | _ -> invalidArg "a" "size mismatch"

    override this.ToString() = 
        this.toArray()
        |> Seq.map (fun value -> value.ToString())
        |> String.concat " "
        |> sprintf "[| %s |]"

    /// Gets whether the direction stored in the present object and the direction in the argument
    /// should be considered equal, allowing for a maximum absolute difference. The function
    /// returns true if the pairwise absolute difference between elements of the two matrices
    /// do not exceed the given threshold value.
    member this.HasSmallAbsoluteDifference (other: Direction3D, maximumAbsoluteDifference) =
        this.X0.HasSmallAbsoluteDifference(other.X0, maximumAbsoluteDifference, "X0.")
        && this.X1.HasSmallAbsoluteDifference(other.X1, maximumAbsoluteDifference, "X1.")
        && this.X2.HasSmallAbsoluteDifference(other.X2, maximumAbsoluteDifference, "X2.")
        
/// All the properties for a 3D volume without the voxel data
[<CLIMutableAttribute>]
type Volume3DProperties = 
    {
        /// The dimensions (size) of the volume.
        Dim: Volume3DDimensions
        /// The voxel spacing.
        Spacing: Tuple3D
        /// The origin of the coordinate system in which the volume lives.
        Origin: Tuple3D
        /// The direction of the coordinate system in which the volume lives.
        Direction: Direction3D
    }

    override this.ToString() = 
        sprintf "{ Dim = %s; Spacing = %s; Origin = %s; Direction = %s }"
            (this.Dim.ToString())
            (this.Spacing.ToString())
            (this.Origin.ToString())
            (this.Direction.ToString())

    /// Creates an empty Volume3D with the properties stored in the present object.
    member x.CreateVolume() = 
        new Volume3D<_>(
            x.Dim.X, x.Dim.Y, x.Dim.Z, 
            x.Spacing.X, x.Spacing.Y, x.Spacing.Z, 
            new Point3D(x.Origin.toArray()), 
            new Matrix3(x.Direction.toArray())
        )

    /// Create Volume3DProperties from a Volume3D
    static member Create(volume:Volume3D<_>) = 
        {
            Dim = { X = volume.DimX; Y = volume.DimY; Z = volume.DimZ}
            Spacing = { X = volume.SpacingX; Y = volume.SpacingY; Z = volume.SpacingZ }
            Origin = Tuple3D.fromArray volume.Origin.Data
            Direction = Direction3D.fromArray volume.Direction.Data
        }

    /// The maximum relative difference allowed between components
    /// of two points, such that they are still considered equal.
    static member MaximumRelativeDifference = 1.0e-4

    /// The maximum absolute difference allowed between elements of two origin tuples,
    /// such that they are still considered equal.
    static member MaximumAbsoluteDifferenceForOrigin = 1.0e-3

    /// The maximum absolute difference allowed between elements of two transformation matrices,
    /// such that they are still considered equal.
    static member MaximumAbsoluteDifferenceForDirection = 1.0e-4

    /// Gets whether the volume properties stored in the present object and 
    /// the volume properties in the argument
    /// should be considered equal, when allowing for small deviations when 
    /// comparing floating point numbers.
    /// Volume dimensions must match exactly. Volume spacing is compared
    /// allowing for a maximum relative difference. Volume origin and direction are compared 
    /// allowing for a maximum absolute difference.
    member this.IsApproximatelyEqual (other: Volume3DProperties) =
        this.Dim = other.Dim
        && this.Spacing.HasSmallRelativeDifference(other.Spacing, Volume3DProperties.MaximumRelativeDifference, "Spacing.")
        && this.Origin.HasSmallAbsoluteDifference(other.Origin, Volume3DProperties.MaximumAbsoluteDifferenceForOrigin, "Origin.")
        && this.Direction.HasSmallAbsoluteDifference(other.Direction, Volume3DProperties.MaximumAbsoluteDifferenceForDirection)

    /// Checks whether all volume properties given in the argument are the same,
    /// using relative or absolute difference for all properties that are of datatype double.
    /// If the properties are not considered equal, throw an ArgumentException. 
    /// When given 0 or 1 volume, the function does nothing.
    static member CheckAllPropertiesEqual (props: seq<Volume3DProperties>) =
        match Seq.toList props with
        | [] 
        | [_] -> ()
        | prop0 :: props ->
            let firstNotEqual = 
                props
                |> Seq.tryFindIndex (fun prop -> 
                    prop0.IsApproximatelyEqual prop
                    |> not
                )
            match firstNotEqual with
            | None -> ()
            | Some index ->
                let first = prop0.ToString()
                let propI = props.[index].ToString()
                let message = sprintf "The volumes do not have the same properties. The first volume has %s, but the volume at index %i has %s" first (index + 1) propI
                ArgumentException message
                |> raise

    /// Checks whether all volume properties given in the argument are the same,
    /// using relative or absolute difference for all properties that are of datatype double.
    /// If the properties are not considered equal, throw an ArgumentException. 
    /// When given 0 or 1 volume, the function does nothing.
    static member CheckAllPropertiesEqual (volumes: seq<Volume3D<_>>) =
        volumes 
        |> Seq.map Volume3DProperties.Create
        |> Volume3DProperties.CheckAllPropertiesEqual

    /// Checks whether all volume properties given in the argument are the same,
    /// using relative or absolute difference for all properties that are of datatype double.
    /// If the properties are not considered equal, throw an ArgumentException. 
    static member CheckAllPropertiesEqual (volume1: Volume3D<'T>, volume2: Volume3D<'U>) =
        let prop1 = Volume3DProperties.Create volume1
        let prop2 = Volume3DProperties.Create volume2
        Volume3DProperties.CheckAllPropertiesEqual [prop1; prop2]
