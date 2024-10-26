using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;
using NiVE3.Numerics;

namespace NiVE3.Expression.Converter
{
    class VectorConverter : IObjectConverter
    {
        public bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue? result)
        {
            switch (value)
            {
                case Vector2 v2:
                    result = new JsArray(engine, [JsNumber.Create(v2.X), JsNumber.Create(v2.Y)]);
                    return true;
                case Vector3 v3:
                    result = new JsArray(engine, [JsNumber.Create(v3.X), JsNumber.Create(v3.Y), JsNumber.Create(v3.Z)]);
                    return true;
                case Vector4 v4:
                    result = new JsArray(engine, [JsNumber.Create(v4.X), JsNumber.Create(v4.Y), JsNumber.Create(v4.Z), JsNumber.Create(v4.W)]);
                    return true;
                case Vector2d v2d:
                    result = new JsArray(engine, [JsNumber.Create(v2d.X), JsNumber.Create(v2d.Y)]);
                    return true;
                case Vector3d v3d:
                    result = new JsArray(engine, [JsNumber.Create(v3d.X), JsNumber.Create(v3d.Y), JsNumber.Create(v3d.Z)]);
                    return true;

                case Vector<byte> vb:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vb.GetElement(i)))]);
                    return true;
                case Vector<sbyte> vsb:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vsb.GetElement(i)))]);
                    return true;
                case Vector<short> vs:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vs.GetElement(i)))]);
                    return true;
                case Vector<ushort> vus:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vus.GetElement(i)))]);
                    return true;
                case Vector<int> vi:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vi.GetElement(i)))]);
                    return true;
                case Vector<uint> vui:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vui.GetElement(i)))]);
                    return true;
                case Vector<long> vl:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vl.GetElement(i)))]);
                    return true;
                case Vector<ulong> vul:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create((decimal)vul.GetElement(i)))]);
                    return true;
                case Vector<float> vf:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vf.GetElement(i)))]);
                    return true;
                case Vector<double> vd:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector<byte>.Count).Select(i => JsNumber.Create(vd.GetElement(i)))]);
                    return true;

                case Vector128<byte> v128b:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128b.GetElement(i)))]);
                    return true;
                case Vector128<sbyte> v128sb:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128sb.GetElement(i)))]);
                    return true;
                case Vector128<short> v128s:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128s.GetElement(i)))]);
                    return true;
                case Vector128<ushort> v128us:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128us.GetElement(i)))]);
                    return true;
                case Vector128<int> v128i:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128i.GetElement(i)))]);
                    return true;
                case Vector128<uint> v128ui:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128ui.GetElement(i)))]);
                    return true;
                case Vector128<long> v128l:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128l.GetElement(i)))]);
                    return true;
                case Vector128<ulong> v128ul:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create((decimal)v128ul.GetElement(i)))]);
                    return true;
                case Vector128<float> v128f:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128f.GetElement(i)))]);
                    return true;
                case Vector128<double> v128d:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector128<byte>.Count).Select(i => JsNumber.Create(v128d.GetElement(i)))]);
                    return true;

                case Vector256<byte> v256b:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256b.GetElement(i)))]);
                    return true;
                case Vector256<sbyte> v256sb:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256sb.GetElement(i)))]);
                    return true;
                case Vector256<short> v256s:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256s.GetElement(i)))]);
                    return true;
                case Vector256<ushort> v256us:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256us.GetElement(i)))]);
                    return true;
                case Vector256<int> v256i:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256i.GetElement(i)))]);
                    return true;
                case Vector256<uint> v256ui:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256ui.GetElement(i)))]);
                    return true;
                case Vector256<long> v256l:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256l.GetElement(i)))]);
                    return true;
                case Vector256<ulong> v256ul:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create((decimal)v256ul.GetElement(i)))]);
                    return true;
                case Vector256<float> v256f:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256f.GetElement(i)))]);
                    return true;
                case Vector256<double> v256d:
                    result = new JsArray(engine, [..Enumerable.Range(0, Vector256<byte>.Count).Select(i => JsNumber.Create(v256d.GetElement(i)))]);
                    return true;
                default:
                    result = null;
                    return false;
            }
        }
    }
}
