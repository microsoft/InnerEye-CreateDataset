///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InnerEye.CreateDataset.Common.Models
{
    /// <summary>
    /// Parameters class to encapsulate the configurations required to perform Geometric Normalization operations
    /// </summary>
    public class GeometricNormalizationParameters
    {
        /// <summary>
        /// Enumeration to encapsulate the differnt kinds of channels that can be provided as input for Geometric Normalization
        /// </summary>
        public enum GeometricNormalizationChannelType
        {
            /// <summary>
            /// Specifies that the channel contains an image, as short volume.
            /// </summary>
            Image,
            /// <summary>
            /// Specifies that the channel contains a ground truth segmentation, as a byte volume.
            /// </summary>
            GroundTruth
        }

        /// <summary>
        /// Wrapper class to encapsulate a single input-output-chanel type config
        /// </summary>
        public class InputOutputChannel
        {
            [JsonRequired]
            public string InputChannelId { get; set; }

            [JsonRequired]
            public string OutputChannelId { get; set; }

            [JsonRequired]
            public GeometricNormalizationChannelType ChannelType { get; set; }

            public InputOutputChannel(string inputChannelId, string outputChannelId, GeometricNormalizationChannelType channelType)
            {
                InputChannelId = inputChannelId;
                OutputChannelId = outputChannelId;
                ChannelType = channelType;
            }

            public InputOutputChannel() { }
        }

        private int _medianFilterRadius;
        private double[] _standardiseSpacings;

        /// <summary>
        /// Flag to determine if Geometric Normalization needs to be performed
        /// </summary>
        [JsonIgnore]
        public bool DoGeometricNormalization { get => StandardiseSpacings != null; }

        /// <summary>
        /// [input channel, output channel, channel type] mappings for each of the channel to process
        /// </summary>
        public InputOutputChannel[] InputOutputChannels { get; set; }

        /// <summary>
        /// Ground truth channels to be loaded (assumed to have a byte datatype)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<InputOutputChannel> GroundTruthChannelsToLoad
        {
            get => GetChannelsToLoad(GeometricNormalizationChannelType.GroundTruth);
        }

        /// <summary>
        /// Image channels to be loaded (assumed to have a short datatype)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<InputOutputChannel> ImageChannelsToLoad
        {
            get => GetChannelsToLoad(GeometricNormalizationChannelType.Image);
        }

        /// <summary>
        ///  Used to define the neighborhood of the cuboid filter of size [x/y/z - radius, x/y/z + radius].
        ///  A radius of 1 will create a neighborhood of size 27 voxels.
        ///  A radius of 0 will result in an identity operation
        /// </summary>
        public int MedianFilterRadius
        {
            get => _medianFilterRadius;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException($"Invalid median filter radius {value} provided, filter radius must be non-negative");
                }

                _medianFilterRadius = value;
            }
        }

        /// <summary>
        /// The spacing that the geometric normalization should produce, as a 3-dimensional array.
        /// Any element of the spacing can be zero, in which case the relevant dimension will not
        /// undergo a change of spacing.
        /// For example, if the input image has spacing (0.5, 0.5, 7), and <see cref="StandardiseSpacings"/>
        /// is set to (1, 1, 0), the output of geometric normalization will have spacing (1, 1, 7).
        /// </summary>
        [JsonRequired]
        public double[] StandardiseSpacings
        {
            get => _standardiseSpacings;
            set
            {
                if (value?.Length != 3)
                {
                    throw new ArgumentException("Spacing parameters must be an array of length 3", nameof(StandardiseSpacings));
                }

                if (value.Any(x => x < 0))
                {
                    throw new ArgumentException("Spacing parameters must be non-negative", nameof(StandardiseSpacings));
                }

                _standardiseSpacings = value;
            }
        }

        /// <summary>
        /// Helper method to parse raw channel configs
        /// </summary>
        private IEnumerable<InputOutputChannel> GetChannelsToLoad(GeometricNormalizationChannelType channelType)
          => InputOutputChannels.Where(x => channelType == x.ChannelType);

        /// <summary>
        ///  Validator for the parameters required to perform Geometric Normalization
        ///  
        ///  Validity conditions: 
        ///  1) InputOutputChannels must be defined and non-empty
        ///  2) InputOutputChannels entries must be of length 3 and must not contain any null entries
        ///  3) StandardiseSpacings must be provided
        ///  4) InputOutputChannels must not contain any unknown GeometricNormalizationChannelType values
        /// </summary>
        public void Validate()
        {
            if (!(InputOutputChannels?.Any()).GetValueOrDefault())
            {
                throw new ArgumentException("InputOutputChannels cannot be null empty");
            }

            if (InputOutputChannels.Any(x => x == null || x.InputChannelId == null || x.OutputChannelId == null))
            {
                throw new ArgumentException("Invalid IO channel configuration found when parsing InputOutputChannels");
            }

            if (!DoGeometricNormalization)
            {
                throw new ArgumentException("StandardiseSpacings cannot be null");
            }
        }
    }
}
