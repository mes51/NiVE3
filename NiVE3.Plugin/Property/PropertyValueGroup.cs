using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Property
{
    /// <summary>
    /// 特定の時間でのプロパティの値を表します
    /// </summary>
    public class PropertyValueGroup
    {
        public static readonly PropertyValueGroup Empty = new PropertyValueGroup(new Dictionary<string, object?>());

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

        internal PropertyValueGroup(Dictionary<string, object?> values)
        {
            Values = values;
        }
    }
}
