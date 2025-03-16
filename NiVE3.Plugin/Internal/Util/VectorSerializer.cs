using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Numerics;

namespace NiVE3.Plugin.Internal.Util
{
    static class VectorSerializer
    {
        public static IDictionary<string, object> Serialize(Vector2 v)
        {
            return new Dictionary<string, object>
            {
                { nameof(Vector2.X), v.X },
                { nameof(Vector2.Y), v.Y }
            };
        }

        public static IDictionary<string, object> Serialize(Vector3 v)
        {
            return new Dictionary<string, object>
            {
                { nameof(Vector3.X), v.X },
                { nameof(Vector3.Y), v.Y },
                { nameof(Vector3.Z), v.Z }
            };
        }

        public static IDictionary<string, object> Serialize(Vector4 v)
        {
            return new Dictionary<string, object>
            {
                { nameof(Vector4.X), v.X },
                { nameof(Vector4.Y), v.Y },
                { nameof(Vector4.Z), v.Z },
                { nameof(Vector4.W), v.W }
            };
        }

        public static IDictionary<string, object> Serialize(Vector2d v)
        {
            return new Dictionary<string, object>
            {
                { nameof(Vector2d.X), v.X },
                { nameof(Vector2d.Y), v.Y }
            };
        }

        public static IDictionary<string, object> Serialize(Vector3d v)
        {
            return new Dictionary<string, object>
            {
                { nameof(Vector3d.X), v.X },
                { nameof(Vector3d.Y), v.Y },
                { nameof(Vector3d.Z), v.Z }
            };
        }

        public static bool TryDeserializeVector2(object? value, out Vector2 result)
        {
            if (value is Vector2 v)
            {
                result = v;
                return true;
            }
            else if (value is IDictionary<string, object> dictionary &&
                     dictionary.TryGetValue(nameof(Vector2.X), out var x) &&
                     dictionary.TryGetValue(nameof(Vector2.Y), out var y))
            {
                result = new Vector2(Convert.ToSingle(x), Convert.ToSingle(y));
                return true;
            }

            result = Vector2.Zero;
            return false;
        }

        public static bool TryDeserializeVector3(object? value, out Vector3 result)
        {
            if (value is Vector3 v)
            {
                result = v;
                return true;
            }
            else if (value is IDictionary<string, object> dictionary &&
                     dictionary.TryGetValue(nameof(Vector3.X), out var x) &&
                     dictionary.TryGetValue(nameof(Vector3.Y), out var y) &&
                     dictionary.TryGetValue(nameof(Vector3.Z), out var z))
            {
                result = new Vector3(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
                return true;
            }

            result = Vector3.Zero;
            return false;
        }

        public static bool TryDeserializeVector4(object? value, out Vector4 result)
        {
            if (value is Vector4 v)
            {
                result = v;
                return true;
            }
            else if (value is IDictionary<string, object> dictionary &&
                     dictionary.TryGetValue(nameof(Vector4.X), out var x) &&
                     dictionary.TryGetValue(nameof(Vector4.Y), out var y) &&
                     dictionary.TryGetValue(nameof(Vector4.Z), out var z) &&
                     dictionary.TryGetValue(nameof(Vector4.W), out var w))
            {
                result = new Vector4(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z), Convert.ToSingle(w));
                return true;
            }

            result = Vector4.Zero;
            return false;
        }

        public static bool TryDeserializeVector2d(object? value, out Vector2d result)
        {
            if (value is Vector2d v)
            {
                result = v;
                return true;
            }
            else if (value is IDictionary<string, object> dictionary &&
                     dictionary.TryGetValue(nameof(Vector2d.X), out var x) &&
                     dictionary.TryGetValue(nameof(Vector2d.Y), out var y))
            {
                result = new Vector2d(Convert.ToDouble(x), Convert.ToDouble(y));
                return true;
            }

            result = Vector2d.Zero;
            return false;
        }

        public static bool TryDeserializeVector3d(object? value, out Vector3d result)
        {
            if (value is Vector3d v)
            {
                result = v;
                return true;
            }
            else if (value is IDictionary<string, object> dictionary &&
                     dictionary.TryGetValue(nameof(Vector3d.X), out var x) &&
                     dictionary.TryGetValue(nameof(Vector3d.Y), out var y) &&
                     dictionary.TryGetValue(nameof(Vector3d.Z), out var z))
            {
                result = new Vector3d(Convert.ToDouble(x), Convert.ToDouble(y), Convert.ToDouble(z));
                return true;
            }

            result = Vector3d.Zero;
            return false;
        }
    }
}
