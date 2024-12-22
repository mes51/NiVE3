using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Plugin.Property
{
    public record KeyFrame(Time Time, object? Value, Ease EaseIn, Ease EaseOut, InterpolationType InterpolationType, int Id)
    {
        // NOTE: xoshiroを使用するため、シードは指定しない
        static readonly Random IdGenerator = new Random();

        public KeyFrame(Time Time, object? Value, Ease EaseIn, Ease EaseOut, InterpolationType InterpolationType) : 
            this(Time, Value, EaseIn, EaseOut, InterpolationType, IdGenerator.Next()) { }
    }
}
