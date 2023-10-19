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

        internal PropertyValueGroup(Dictionary<string, object?> values)
        {
            Values = values;
        }
    }
}
