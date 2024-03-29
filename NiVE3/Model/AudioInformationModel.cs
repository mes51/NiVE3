using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class AudioInformationModel : BindableBase
    {
        private double leftAudioLevel = double.NegativeInfinity;
        public double LeftAudioLevel
        {
            get { return leftAudioLevel; }
            set { SetProperty(ref leftAudioLevel, value); }
        }

        private double rightAudioLevel = double.NegativeInfinity;
        public double RightAudioLevel
        {
            get { return rightAudioLevel; }
            set { SetProperty(ref rightAudioLevel, value); }
        }

        public void CalcAudioLevel(ReadOnlySpan<float> audio)
        {
            var leftMax = double.NegativeInfinity;
            var rightMax = double.NegativeInfinity;

            var startPos = 0;
            if (Vector<float>.IsSupported)
            {
                var audioVector = MemoryMarshal.Cast<float, Vector<float>>(audio[..((audio.Length / Vector<float>.Count) * Vector<float>.Count)]);
                var maxVector = new Vector<float>(float.NegativeInfinity);

                for (var i = 0; i < audioVector.Length; i++)
                {
                    maxVector = Vector.Max(maxVector, audioVector[i]);
                }

                for (var i = 0; i < Vector<float>.Count; i += 2)
                {
                    leftMax = Math.Max(leftMax, maxVector[i]);
                    rightMax = Math.Max(rightMax, maxVector[i + 1]);
                }

                startPos = audioVector.Length * Vector<float>.Count;
            }

            for (var i = startPos; i < audio.Length; i += 2)
            {
                leftMax = Math.Max(leftMax, audio[i]);
                rightMax = Math.Max(rightMax, audio[i + 1]);
            }

            LeftAudioLevel = 20.0 * Math.Log10(Math.Abs(leftMax));
            RightAudioLevel = 20.0 * Math.Log10(Math.Abs(rightMax));
        }

        public void ClearLevel()
        {
            LeftAudioLevel = double.NegativeInfinity;
            RightAudioLevel = double.NegativeInfinity;
        }
    }
}
