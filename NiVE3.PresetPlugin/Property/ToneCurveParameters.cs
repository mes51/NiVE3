using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Shared.Extension;

namespace NiVE3.PresetPlugin.Property
{
    class ToneCurveParameters : IEquatable<ToneCurveParameters>
    {
        public ToneCurvePoint[] Rgb { get; }

        public ToneCurvePoint[] R { get; }

        public ToneCurvePoint[] G { get; }

        public ToneCurvePoint[] B { get; }

        public ToneCurvePoint[] A { get; }

        public ToneCurveParameters(ToneCurvePoint[] rgb, ToneCurvePoint[] r, ToneCurvePoint[] g, ToneCurvePoint[] b, ToneCurvePoint[] a)
        {
            Rgb = rgb;
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public ToneCurveParameters(IEnumerable<ToneCurvePoint> rgb, IEnumerable<ToneCurvePoint> r, IEnumerable<ToneCurvePoint> g, IEnumerable<ToneCurvePoint> b, IEnumerable<ToneCurvePoint> a)
        {
            Rgb = rgb as ToneCurvePoint[] ?? rgb.ToArray();
            R = r as ToneCurvePoint[] ?? r.ToArray();
            G = g as ToneCurvePoint[] ?? g.ToArray();
            B = b as ToneCurvePoint[] ?? b.ToArray();
            A = a as ToneCurvePoint[] ?? a.ToArray();
        }

        public static readonly ToneCurveParameters Empty = new ToneCurveParameters(
            [new ToneCurvePoint(0.0F, 0.0F), new ToneCurvePoint(1.0F, 1.0F)],
            [new ToneCurvePoint(0.0F, 0.0F), new ToneCurvePoint(1.0F, 1.0F)],
            [new ToneCurvePoint(0.0F, 0.0F), new ToneCurvePoint(1.0F, 1.0F)],
            [new ToneCurvePoint(0.0F, 0.0F), new ToneCurvePoint(1.0F, 1.0F)],
            [new ToneCurvePoint(0.0F, 0.0F), new ToneCurvePoint(1.0F, 1.0F)]
        );

        public object Serialize()
        {
            return new Dictionary<string, object>
            {
                { nameof(Rgb), Rgb.Select(p => p.Serialize()).ToArray() },
                { nameof(R), R.Select(p => p.Serialize()).ToArray() },
                { nameof(G), G.Select(p => p.Serialize()).ToArray() },
                { nameof(B), B.Select(p => p.Serialize()).ToArray() },
                { nameof(A), A.Select(p => p.Serialize()).ToArray() }
            };
        }

        public static ToneCurveParameters? Deserialize(object? serializedValue)
        {
            if (serializedValue is not IDictionary<string, object> dictionary ||
                !dictionary.TryGetValue(nameof(Rgb), out object[]? serializedRgb) ||
                !dictionary.TryGetValue(nameof(R), out object[]? serializedR) ||
                !dictionary.TryGetValue(nameof(G), out object[]? serializedG) ||
                !dictionary.TryGetValue(nameof(B), out object[]? serializedB) ||
                !dictionary.TryGetValue(nameof(A), out object[]? serializedA) ||
                serializedRgb.Length < 2 ||
                serializedR.Length < 2 ||
                serializedG.Length < 2 ||
                serializedB.Length < 2 ||
                serializedA.Length < 2)
            {
                return null;
            }

            var rgb = serializedRgb.Select(ToneCurvePoint.Deserialize).NonNull().DistinctBy(p => p.InValue);
            var r = serializedRgb.Select(ToneCurvePoint.Deserialize).NonNull().DistinctBy(p => p.InValue);
            var g = serializedRgb.Select(ToneCurvePoint.Deserialize).NonNull().DistinctBy(p => p.InValue);
            var b = serializedRgb.Select(ToneCurvePoint.Deserialize).NonNull().DistinctBy(p => p.InValue);
            var a = serializedRgb.Select(ToneCurvePoint.Deserialize).NonNull().DistinctBy(p => p.InValue);

            return new ToneCurveParameters(rgb, r, g, b, a);
        }

        public bool Equals(ToneCurveParameters? other)
        {
            if (other == null)
            {
                return false;
            }

            return Rgb.Length == other.Rgb.Length &&
                R.Length == other.R.Length &&
                G.Length == other.G.Length &&
                B.Length == other.B.Length &&
                A.Length == other.A.Length &&
                Rgb.SequenceEqual(other.Rgb) &&
                R.SequenceEqual(other.R) &&
                G.SequenceEqual(other.G) &&
                B.SequenceEqual(other.B) &&
                A.SequenceEqual(other.A);
        }

        public override bool Equals(object? obj)
        {
            if (obj is  ToneCurveParameters other)
            {
                return Equals(other);
            }
            else
            {
                return false;
            }
        }

        public override string? ToString()
        {
            return $$"""
            {
                RGB = [{{(string.Join("", Rgb.Select(p => $"{{ {p.InValue}, {p.OutValue} }}")))}}],
                R = [{{(string.Join("", R.Select(p => $"{{ {p.InValue}, {p.OutValue} }}")))}}],
                G = [{{(string.Join("", G.Select(p => $"{{ {p.InValue}, {p.OutValue} }}")))}}],
                B = [{{(string.Join("", B.Select(p => $"{{ {p.InValue}, {p.OutValue} }}")))}}],
                A = [{{(string.Join("", A.Select(p => $"{{ {p.InValue}, {p.OutValue} }}")))}}] 
            }
            """;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            for (var i = 0; i < Rgb.Length; i++)
            {
                hashCode.Add(Rgb[i]);
            }
            for (var i = 0; i < R.Length; i++)
            {
                hashCode.Add(R[i]);
            }
            for (var i = 0; i < G.Length; i++)
            {
                hashCode.Add(G[i]);
            }
            for (var i = 0; i < B.Length; i++)
            {
                hashCode.Add(B[i]);
            }
            for (var i = 0; i < A.Length; i++)
            {
                hashCode.Add(A[i]);
            }

            return hashCode.ToHashCode();
        }
    }

    record ToneCurvePoint(float InValue, float OutValue)
    {
        public object Serialize()
        {
            return this;
        }

        public static ToneCurvePoint? Deserialize(object? value)
        {
            if (value is ToneCurvePoint toneCurvePoint)
            {
                return toneCurvePoint;
            }
            else if (value is IDictionary<string, object> dictionary && dictionary.TryGetValue(nameof(InValue), out float inValue) && dictionary.TryGetValue(nameof(OutValue), out float outValue))
            {
                return new ToneCurvePoint(inValue, inValue);
            }
            else
            {
                return null;
            }
        }
    }
}
