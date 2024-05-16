using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.Property.Types;

namespace NiVE3.Plugin.Property
{
    /// <summary>
    /// 特定の時間でのプロパティの値を表します
    /// </summary>
    public class PropertyValueGroup
    {
        public static readonly PropertyValueGroup Empty = new PropertyValueGroup("", [], []);

        /// <summary>
        /// このプロパティグループのIDを取得します
        /// </summary>
        public string PropertyGroupId { get; }

        /// <summary>
        /// プロパティの値を取得します
        /// </summary>
        /// <param name="propertyId">取得するプロパティ、またはプロパティグループのID</param>
        /// <returns>プロパティの値、またはプロパティグループ、該当するIDの値が存在しない場合はnull</returns>
        public object? this[string propertyId]
        {
            get
            {
                if (Values.TryGetValue(propertyId, out var value))
                {
                    return value;
                }
                else
                {
                    return null;
                }
            }
        }

        Dictionary<string, IPropertyType> PropertyTypes { get; }

        Dictionary<string, object?> Values { get; }

        /// <summary>
        /// 指定したプロパティ、またはグループのIDの値を取得します
        /// </summary>
        /// <param name="propertyId">取得するプロパティ、またはグループのID</param>
        /// <param name="value">取得した値</param>
        /// <returns>値を取得できた場合はtrue、なかった場合はfalse</returns>
        public bool TryGetValue(string propertyId, out object? value)
        {
            return Values.TryGetValue(propertyId, out value);
        }

        /// <summary>
        /// 指定したプロパティ、またはグループのIDの値を取得します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="propertyId">取得するプロパティ、またはグループのID</param>
        /// <param name="value">取得した値</param>
        /// <returns>値を取得、かつ指定した型だった場合はtrue、なかった場合はfalse</returns>
        public bool TryGetValue<T>(string propertyId, out T? value)
        {
            var result = Values.TryGetValue(propertyId, out var obj);
            if (result && obj is T castedValue)
            {
                value = castedValue;
                return true;
            }
            else if (result && typeof(T).IsClass)
            {
                value = default;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 指定したプロパティ、またはグループのIDをグループ内から検索して取得します
        /// </summary>
        /// <param name="propertyId">検索するプロパティ、またはグループのID</param>
        /// <param name="value">取得した値</param>
        /// <returns>値を取得できた場合はtrue、なかった場合はfalse</returns>
        public bool TryGetValueInTree(string propertyId, out object? value)
        {
            if (Values.TryGetValue(propertyId, out var result))
            {
                value = result;
                return true;
            }

            foreach (var child in Values.Values.OfType<PropertyValueGroup>())
            {
                if (child.TryGetValueInTree(propertyId, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// 指定したプロパティ、またはグループのIDをグループ内から検索して取得します
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="propertyId">検索するプロパティ、またはグループのID</param>
        /// <param name="value">取得した値</param>
        /// <returns>値を取得、かつ指定した型だった場合はtrue、なかった場合はfalse</returns>
        public bool TryGetValueInTree<T>(string propertyId, out T? value)
        {
            var result = TryGetValueInTree(propertyId, out var obj);
            if (result && obj is T castedValue)
            {
                value = castedValue;
                return true;
            }
            else if (result && typeof(T).IsClass)
            {
                value = default;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        internal Int128 CalcHash()
        {
            var hash = new XxHash3();
            TraverseCalcHash(hash);

            var result = (Int128)0;
            var resultSpan = MemoryMarshal.CreateSpan(ref result, 1);
            hash.GetCurrentHash(MemoryMarshal.Cast<Int128, byte>(resultSpan));

            return result;
        }

        private void TraverseCalcHash(XxHash3 hash)
        {
            foreach (var (key, value) in Values)
            {
                var pt = PropertyTypes[key];
                switch (pt)
                {
                    case PropertyGroupType:
                        if (value != null)
                        {
                            ((PropertyValueGroup)value).TraverseCalcHash(hash);
                        }
                        break;
                    case AppendablePropertyType:
                        if (value is PropertyValueGroup[] children)
                        {
                            foreach (var c in children)
                            {
                                c.TraverseCalcHash(hash);
                            }
                        }
                        break;
                    default:
                        hash.Append(pt.ConvertToHashBase(value));
                        break;
                }
            }
        }

        internal PropertyValueGroup(string propertyGroupId, Dictionary<string, object?> values, Dictionary<string, IPropertyType> propertyTypes)
        {
            PropertyGroupId = propertyGroupId;
            PropertyTypes = propertyTypes;
            Values = values;
        }
    }
}
