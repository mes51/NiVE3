using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Plugin.Property
{
    public record KeyFrame(double Time, object? Value, Ease EaseIn, Ease EaseOut, InterpolationType InterpolationType, int Id)
    {
        // NOTE: xoshiroを使用するため、シードは指定しない
        static readonly Random IdGenerator = new Random();

        public KeyFrame(double Time, object? Value, Ease EaseIn, Ease EaseOut, InterpolationType InterpolationType) : 
            this(Time, Value, EaseIn, EaseOut, InterpolationType, IdGenerator.Next()) { }
    }
}
