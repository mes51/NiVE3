using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Data.Config
{
    class CompositionPresetData : IEquatable<CompositionPresetData>
    {
        public static readonly CompositionPresetData DefaultCompositionSetting = new CompositionPresetData
        {
            Name = "FullHD 30fps",
            Width = 1920,
            Height = 1080,
            FrameRate = 30.0,
            ShutterAngle = 180,
            ShutterPhase = 180,
            MotionBlurSampleCount = 16
        };

        public string Name { get; set; } = "";

        public int Width { get; set; }

        public int Height { get; set; }

        public double FrameRate { get; set; }

        public bool IsRetentionFrameRate { get; set; }

        public bool ApplyToneMappingWhenNested { get; set; }

        public int ShutterAngle { get; set; }

        public int ShutterPhase { get; set; }

        public int MotionBlurSampleCount { get; set; }

        public bool IsSame(CompositionPresetData other)
        {
            return Width == other.Width &&
                Height == other.Height &&
                FrameRate == other.FrameRate &&
                IsRetentionFrameRate == other.IsRetentionFrameRate &&
                ApplyToneMappingWhenNested == other.ApplyToneMappingWhenNested &&
                ShutterAngle == other.ShutterAngle &&
                ShutterPhase == other.ShutterPhase &&
                MotionBlurSampleCount == other.MotionBlurSampleCount;
        }

        public bool Equals(CompositionPresetData? other)
        {
            return other != null && Name == other.Name && IsSame(other);
        }

        public override bool Equals(object? obj)
        {
            if (obj is CompositionPresetData data)
            {
                return Equals(data);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name);
            hashCode.Add(Width);
            hashCode.Add(Height);
            hashCode.Add(FrameRate);
            hashCode.Add(IsRetentionFrameRate);
            hashCode.Add(ApplyToneMappingWhenNested);
            hashCode.Add(ShutterAngle);
            hashCode.Add(ShutterPhase);
            hashCode.Add(MotionBlurSampleCount);

            return hashCode.ToHashCode();
        }

        public override string? ToString()
        {
            return $"{Name} <{Width}, {Height}, {FrameRate:F2}, {IsRetentionFrameRate}, {ApplyToneMappingWhenNested}, {ShutterAngle}, {ShutterPhase}, {MotionBlurSampleCount}>";
        }
    }
}
