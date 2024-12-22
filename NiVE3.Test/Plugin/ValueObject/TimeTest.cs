using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Test.Plugin.ValueObject
{
    public class TimeTest
    {
        const int TimeDigit = 10;

        const int FrameRateDigit = 2;

        [Test]
        public void TestCreateFromFrameAndFrameRate()
        {
            var time = new Time(1, 30.0);
            Assert.Multiple(() =>
            {
                Assert.That(time.RealTime, Is.NaN);
                Assert.That(time.IsFrameTime, Is.True);
                Assert.That(time.FrameRateIsInteger, Is.True);
            });
        }

        [Test]
        public void TestCreateFromFrameAndNonIntegerFrameRate()
        {
            var time = new Time(1, 29.97);
            Assert.Multiple(() =>
            {
                Assert.That(time.RealTime, Is.NaN);
                Assert.That(time.IsFrameTime, Is.True);
                Assert.That(time.FrameRateIsInteger, Is.False);
            });
        }

        [Test]
        public void TestCreateFromRealTime()
        {
            var time = new Time(1.0);
            Assert.Multiple(() =>
            {
                Assert.That(time.RealTime, Is.Not.NaN);
                Assert.That(time.IsFrameTime, Is.False);
                Assert.That(time.FrameRateIsInteger, Is.False);
            });
        }

        [Test]
        public void TestCreateThrowExceptionWhenNaNFrameRate()
        {
            Assert.Throws<ArgumentException>(() => new Time(1, double.NaN));
        }

        [Test]
        public void TestCreateThrowExceptionWhenInfinityFrameRate()
        {
            Assert.Throws<ArgumentException>(() => new Time(1, double.PositiveInfinity));
        }

        [Test]
        public void TestCreateThrowExceptionWhenLTEZeroFrameRate()
        {
            Assert.Throws<ArgumentException>(() => new Time(1, 0.0));
        }

        [Test]
        public void TestFromTimeIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var actual = Time.FromTime(1.5, FrameRate);

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(45));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestFromTimeNonIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 29.97;
                var actual = Time.FromTime(100.0, FrameRate);

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.Frame, Is.EqualTo(2997));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestFromTimeNonIntegerFrame()
        {
            Assert.Multiple(() =>
            {
                const double RealTime = 0.2224;
                const double FrameRate = 30;
                var actual = Time.FromTime(RealTime, FrameRate);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(RealTime));
            });
        }

        [Test]
        public void TestFromTimeThrowExceptionWhenInvalidFrameRate()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => Time.FromTime(1.0, 0.0));
                Assert.Throws<ArgumentException>(() => Time.FromTime(1.0, double.NaN));
                Assert.Throws<ArgumentException>(() => Time.FromTime(1.0, double.PositiveInfinity));
            });
        }

        [Test]
        public void TestEquals()
        {
            Assert.Multiple(() =>
            {
                var time = new Time(1, 30.0);
                Assert.That(time, Is.EqualTo(new Time(1, 30.0)));
                Assert.That(time, Is.EqualTo(new Time(2, 60.0)));
                Assert.That(time, Is.EqualTo(new Time(Math.Round(1.0 / 30.0, TimeDigit))));
            });
        }

        [Test]
        public void TestNotEquals()
        {
            Assert.Multiple(() =>
            {
                var time = new Time(1, 30.0);

                Assert.That(time, Is.Not.EqualTo(new Time(2, 30.0)));
                Assert.That(time, Is.Not.EqualTo(new Time(1, 45.0)));
                Assert.That(time, Is.Not.EqualTo(new Time(Math.Round(1.0 / 30.0, TimeDigit) + Math.Pow(0.1, TimeDigit - 1))));
                Assert.That(time, Is.Not.EqualTo(new Time(double.NaN)));
            });
        }

        [Test]
        public void TestCastToDouble()
        {
            var time = new Time(1, 30.0);

            Assert.That((double)time, Is.EqualTo(Math.Round(1.0 / 30.0, TimeDigit)));
        }

        [Test]
        public void TestCastFromDouble()
        {
            var realTime = Math.Round(1.0 / 30.0, TimeDigit);

            Assert.That((Time)realTime, Is.EqualTo(new Time(realTime)));
        }

        [Test]
        public void TestUnaryPlus()
        {
            Assert.Multiple(() =>
            {
                var time = new Time(1, 30.0);
                var realTime = new Time(1.0);

                Assert.That(+time, Is.EqualTo(time));
                Assert.That(+realTime, Is.EqualTo(realTime));
            });
        }

        [Test]
        public void TestUnaryMinus()
        {
            Assert.Multiple(() =>
            {
                var time = new Time(1, 30.0);
                var realTime = new Time(1.0);

                Assert.That(-time, Is.EqualTo(new Time(-1, 30.0)));
                Assert.That(-realTime, Is.EqualTo(new Time(-1.0)));
            });
        }

        [Test]
        public void TestIncrementIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                const int FrameRate = 30;
                var time = new Time(1, FrameRate);
                time++;

                Assert.That(time.IsFrameTime, Is.True);
                Assert.That(time.FrameRateIsInteger, Is.True);
                Assert.That(time.Frame, Is.EqualTo(FrameRate + 1));
                Assert.That(time.FrameRate, Is.EqualTo((double)FrameRate));
            });
        }

        [Test]
        public void TestIncrementNonIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 29.97;
                var time = new Time(1, FrameRate);
                time++;

                Assert.That(time.IsFrameTime, Is.False);
                Assert.That(time.FrameRateIsInteger, Is.False);
                Assert.That(time.RealTime, Is.EqualTo(Math.Round(1.0 + 1 / FrameRate, TimeDigit)));
            });
        }

        [Test]
        public void TestIncrementRealTime()
        {
            var time = new Time(1.0);
            time++;

            Assert.That(time, Is.EqualTo(new Time(2.0)));
        }

        [Test]
        public void TestDecrementIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                const int FrameRate = 30;
                var time = new Time(1, FrameRate);
                time--;

                Assert.That(time.IsFrameTime, Is.True);
                Assert.That(time.FrameRateIsInteger, Is.True);
                Assert.That(time.Frame, Is.EqualTo(1 - FrameRate));
                Assert.That(time.FrameRate, Is.EqualTo((double)FrameRate));
            });
        }

        [Test]
        public void TestDecrementNonIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 29.97;
                var time = new Time(1, FrameRate);
                time--;

                Assert.That(time.IsFrameTime, Is.False);
                Assert.That(time.FrameRateIsInteger, Is.False);
                Assert.That(time.RealTime, Is.EqualTo(Math.Round(1 / FrameRate - 1.0, TimeDigit)));
            });
        }

        [Test]
        public void TestDecrementRealTime()
        {
            var time = new Time(1.0);
            time--;

            Assert.That(time, Is.EqualTo(new Time(0.0)));
        }

        #region Add

        [Test]
        public void TestAddSameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, FrameRate);
                var b = new Time(3, FrameRate);
                var actual = a + b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(4));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestAddDifferentFrameRateTimeAndResultIsSameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 45.0;
                var a = new Time(3, FrameRateA);
                var b = new Time(9, FrameRateB);
                var actual = a + b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(9));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRateA));
            });
        }

        [Test]
        public void TestAddDifferentFrameRateTimeAndDenomination()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 21.0;
                var a = new Time(10, FrameRateA);
                var b = new Time(13, FrameRateB);
                var actual = a + b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(20));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRateB));
            });
        }

        [Test]
        public void TestAddDifferentFrameRateTimeAndNotDenomination()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 31.0;
                const double FrameRateB = 21.0;
                const double ExpectedFrameRate = 651.0;
                var a = new Time(1, FrameRateA);
                var b = new Time(1, FrameRateB);
                var actual = a + b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo((int)(ExpectedFrameRate / FrameRateA) + (int)(ExpectedFrameRate / FrameRateB)));
                Assert.That(actual.FrameRate, Is.EqualTo(651.0));
            });
        }

        [Test]
        public void TestAddNonIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                var FrameRateA = 30.0;
                var FrameRateB = 29.97;
                var a = new Time(1, FrameRateA);
                var b = new Time(1, FrameRateB);
                var actual = a + b;
                var expectedTime = Math.Round(1.0 / FrameRateA + 1.0 / FrameRateB, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a + b).RealTime, Is.EqualTo(expectedTime));
                Assert.That((b + a).RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestAddRealTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = new Time(1.0);
                var actual = a + b;
                var expectedTime = Math.Round(1.0 + 1.0 / FrameRate, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a + b).RealTime, Is.EqualTo(expectedTime));
                Assert.That((b + a).RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestAddBothRealTime()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.32);
                var b = new Time(1.0);
                var actual = a + b;
                var expectedTime = Math.Round(a.RealTime + b.RealTime, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a + b).RealTime, Is.EqualTo(expectedTime));
                Assert.That((b + a).RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestAddZero()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, FrameRate);
                var actual = a + Time.Zero;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(1));
                Assert.That((a + Time.Zero).FrameRate, Is.EqualTo(FrameRate));
                Assert.That((Time.Zero + a).FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestAddDoubleIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = 1.0;
                var actual = a + b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(1 + (int)FrameRate));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestAddDoubleNonIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = 0.12;
                var actual = a + b;
                var expectedTime = Math.Round(b + 1.0 / FrameRate, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestAddRealTimeAndDouble()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.4);
                var b = 0.12;
                var actual = a + b;
                var expectedTime = Math.Round(b + a.RealTime, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        #endregion Add

        #region Subtract

        [Test]
        public void TestSubtractSameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, FrameRate);
                var b = new Time(3, FrameRate);
                var actual = a - b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(-2));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestSubtractDifferentFrameRateTimeAndResultIsSameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 45.0;
                var a = new Time(1, FrameRateA);
                var b = new Time(3, FrameRateB);
                var actual = a - b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(-1));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRateA));
            });
        }

        [Test]
        public void TestSubtractDifferentFrameRateTimeAndDenomination()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 21.0;
                var a = new Time(10, FrameRateA);
                var b = new Time(13, FrameRateB);
                var actual = a - b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(-2));
                Assert.That(actual.FrameRate, Is.EqualTo(7.0));
            });
        }

        [Test]
        public void TestSubtractDifferentFrameRateTimeAndNotDenomination()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 31.0;
                const double FrameRateB = 21.0;
                const double ExpectedFrameRate = 651.0;
                var a = new Time(1, FrameRateA);
                var b = new Time(1, FrameRateB);
                var actual = a - b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo((int)(ExpectedFrameRate / FrameRateA) - (int)(ExpectedFrameRate / FrameRateB)));
                Assert.That(actual.FrameRate, Is.EqualTo(651.0));
            });
        }

        [Test]
        public void TestSubtractNonIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                var FrameRateA = 30.0;
                var FrameRateB = 29.97;
                var a = new Time(1, FrameRateA);
                var b = new Time(1, FrameRateB);
                var actual = a - b;
                var expectedTimeA = Math.Round(1.0 / FrameRateA - 1.0 / FrameRateB, TimeDigit);
                var expectedTimeB = Math.Round(1.0 / FrameRateB - 1.0 / FrameRateA, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a - b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b - a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        [Test]
        public void TestSubtractRealTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = new Time(1.0);
                var actual = a - b;
                var expectedTimeA = Math.Round(1.0 / FrameRate - 1.0, TimeDigit);
                var expectedTimeB = Math.Round(1.0 - 1.0 / FrameRate, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a - b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b - a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        [Test]
        public void TestSubtractBothRealTime()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.32);
                var b = new Time(1.0);
                var actual = a - b;
                var expectedTimeA = Math.Round(a.RealTime - b.RealTime, TimeDigit);
                var expectedTimeB = Math.Round(b.RealTime - a.RealTime, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a - b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b - a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        [Test]
        public void TestSubtractZero()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, FrameRate);
                var actual = a - Time.Zero;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(1));
                Assert.That((a - Time.Zero).FrameRate, Is.EqualTo(FrameRate));
                Assert.That((Time.Zero - a).FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestSubtractDoubleIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = 1.0;
                var actual = a - b;
                var expectedFrameA = a.Frame - (int)FrameRate;
                var expectedFrameB = (int)FrameRate - a.Frame;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
                Assert.That((a - b).Frame, Is.EqualTo(expectedFrameA));
                Assert.That((b - a).Frame, Is.EqualTo(expectedFrameB));
            });
        }

        [Test]
        public void TestSubtractDoubleNonIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = 0.12;
                var actual = a - b;
                var expectedTimeA = Math.Round(1.0 / FrameRate - b, TimeDigit);
                var expectedTimeB = Math.Round(b - 1.0 / FrameRate, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a - b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b - a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        [Test]
        public void TestSubtractRealTimeAndDouble()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.4);
                var b = 0.12;
                var actual = a - b;
                var expectedTimeA = Math.Round(a.RealTime - b, TimeDigit);
                var expectedTimeB = Math.Round(b - a.RealTime, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a - b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b - a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        #endregion Subtract

        #region Multiply

        [Test]
        public void TestMultiplySameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(2, FrameRate);
                var b = new Time(3, FrameRate);
                var actual = a * b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(6));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestMultiplyDifferentFrameRateTimeAndResultIsSameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 21.0;
                var a = new Time(2, FrameRateA);
                var b = new Time(2, FrameRateB);
                var actualA = a * b;
                var actualB = b * a;

                Assert.That(actualA.IsFrameTime, Is.True);
                Assert.That(actualA.FrameRateIsInteger, Is.True);
                Assert.That(actualA.Frame, Is.EqualTo(40));
                Assert.That(actualA.FrameRate, Is.EqualTo(FrameRateA));
                Assert.That(actualB.Frame, Is.EqualTo(28));
                Assert.That(actualB.FrameRate, Is.EqualTo(FrameRateB));
            });
        }

        [Test]
        public void TestMultiplyNonIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                var FrameRateA = 30.0;
                var FrameRateB = 29.97;
                var a = new Time(3, FrameRateA);
                var b = new Time(2997, FrameRateB);
                var actual = a * b;
                var expectedTimeA = Math.Round((a.Frame / FrameRateA) * (b.Frame / FrameRateB), TimeDigit);
                var expectedTimeB = Math.Round((b.Frame / FrameRateB) * (a.Frame / FrameRateA), TimeDigit);

                // NOTE: 計算後時間は10秒になるが、30fps300フレームではなく実時間になるのは仕様通り
                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a * b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b * a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        [Test]
        public void TestMultiplyRealTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = new Time(2.0);
                var actual = a - b;
                var expectedTimeA = Math.Round(1.0 / FrameRate * b.RealTime, TimeDigit);
                var expectedTimeB = Math.Round(b.RealTime * 1.0 / FrameRate, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a * b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b * a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        [Test]
        public void TestMultiplyBothRealTime()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.32);
                var b = new Time(2.0);
                var actual = a * b;
                var expectedTime = Math.Round(a.RealTime * b.RealTime, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a * b).RealTime, Is.EqualTo(expectedTime));
                Assert.That((b * a).RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestMultiplyZero()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, FrameRate);

                Assert.That(a * Time.Zero, Is.EqualTo(Time.Zero));
                Assert.That(Time.Zero * a, Is.EqualTo(Time.Zero));
            });
        }

        [Test]
        public void TestMultiplyOne()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, FrameRate);

                Assert.That(a * Time.One, Is.EqualTo(a));
                Assert.That(Time.One * a, Is.EqualTo(a));
            });
        }

        [Test]
        public void TestMultiplyDoubleIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = 2.0;
                var actual = a * b;
                var expectedFrame = a.Frame * (int)b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
                Assert.That(actual.Frame, Is.EqualTo(expectedFrame));
            });
        }

        [Test]
        public void TestMultiplyDoubleNonIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(10, 30.0);
                var b = 2.01;
                var actual = a * b;
                var expectedTime = Math.Round(a.Frame / FrameRate * b, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestMultiplyRealTimeAndDouble()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.4);
                var b = 0.12;
                var actual = a * b;
                var expectedTime = Math.Round(a.RealTime * b, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestMultiplyDoubleZero()
        {
            Assert.That(new Time(1, 30.0) * 0.0, Is.EqualTo(Time.Zero));
        }

        [Test]
        public void TestMultiplyDoubleOne()
        {
            var a = new Time(1, 30.0);

            Assert.That(a * 1.0, Is.EqualTo(a));
        }

        [Test]
        public void TestMultiplyIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(1, 30.0);
                var b = 3;
                var actual = a * b;
                var expectedFrame = a.Frame * b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
                Assert.That(actual.Frame, Is.EqualTo(expectedFrame));
            });
        }

        [Test]
        public void TestMultiplyRealTimeAndInteger()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.4);
                var b = 5;
                var actual = a * b;
                var expectedTime = Math.Round(a.RealTime * b, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        #endregion Multiply

        #region Divide

        [Test]
        public void TestDivideSameFrameRateNonReminder()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(6, FrameRate);
                var b = new Time(3, FrameRate);
                var actual = a / b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(2));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestDivideSameFrameRateWithReminder()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(5, FrameRate);
                var b = new Time(3, FrameRate);
                var actual = a / b;
                var expectedTime = Math.Round(5.0 / 3.0, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestDivideDifferentFrameRateTimeAndResultIsSameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 45.0;
                var a = new Time(18, FrameRateA);
                var b = new Time(3, FrameRateB);
                var actual = a / b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(3));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRateA));
            });
        }

        [Test]
        public void TestDivideDifferentFrameRateTimeAndDenomination()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 21.0;
                const double LcdFrameRate = 210.0;
                var a = new Time(400, FrameRateA);
                var b = new Time(7, FrameRateB);
                var actual = a / b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(4));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRateB));
                Assert.That(actual.Frame / actual.FrameRate, Is.EqualTo(40.0 / LcdFrameRate));
            });
        }

        [Test]
        public void TestDivideDifferentFrameRateWithReminder()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 5.0;
                const double FrameRateB = 9.0;
                var a = new Time(22, FrameRateA);
                var b = new Time(4, FrameRateB);
                var actual = a / b;
                var exprectedTime = Math.Round((a.Frame / FrameRateA) / (b.Frame / FrameRateB), TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(exprectedTime));
            });
        }

        [Test]
        public void TestDivideNonIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                var FrameRateA = 30.0;
                var FrameRateB = 29.97;
                var a = new Time(3000, FrameRateA);
                var b = new Time(2997, FrameRateB);
                var actual = a / b;
                var exprectedTime = Math.Round((a.Frame / FrameRateA) / (b.Frame / FrameRateB), TimeDigit);

                // NOTE: 計算後時間は1秒になるが、30fps30フレームではなく実時間になるのは仕様通り
                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(exprectedTime));
            });
        }

        [Test]
        public void TestDivideRealTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(15, 30.0);
                var b = new Time(0.25);
                var actual = a / b;
                var expectedTimeA = Math.Round((a.Frame / FrameRate) / b.RealTime, TimeDigit);
                var expectedTimeB = Math.Round(b.RealTime / (a.Frame / FrameRate), TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a / b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b / a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        [Test]
        public void TestDivideBothRealTime()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.32);
                var b = new Time(0.8);
                var actual = a / b;
                var expectedTime = Math.Round(a.RealTime / b.RealTime, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestDivideZero()
        {
            Assert.Throws<DivideByZeroException>(() => _ = new Time(1.0) / Time.Zero);
        }

        [Test]
        public void TestDivideOne()
        {
            Assert.Multiple(() =>
            {
                var time = new Time(1, 30.0);

                Assert.That(time / Time.One, Is.EqualTo(time));
                Assert.That(time / new Time(40, 40.0), Is.EqualTo(time));
            });
        }

        [Test]
        public void TestDivideDoubleIntegerTimeNonReminder()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(10, 30.0);
                var b = 2.0;
                var actual = a / b;
                var expectedFrame = a.Frame / (int)b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
                Assert.That(actual.Frame, Is.EqualTo(expectedFrame));
            });
        }

        [Test]
        public void TestDivideDoubleNonIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(5, 30.0);
                var b = 0.12;
                var actual = a / b;
                var expectedTime = Math.Round((a.Frame / FrameRate) / b, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestDivideRealTimeAndDouble()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.4);
                var b = 0.12;
                var actual = a / b;
                var expectedTime = Math.Round(a.RealTime / b, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestDivideDoubleZero()
        {
            Assert.Throws<DivideByZeroException>(() => _ = new Time(1, 30.0) / 0.0);
        }

        [Test]
        public void TestDivideDoubleByTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = 0.12;
                var b = new Time(4, FrameRate);
                var actual = a / b;
                var expectedTime = Math.Round(a / (b.Frame / FrameRate), TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        #endregion Divide

        #region Modulo

        [Test]
        public void TestModuloSameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(7, FrameRate);
                var b = new Time(4, FrameRate);
                var actual = a % b;
                var expectedFrame = a.Frame % b.Frame;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(expectedFrame));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
            });
        }

        [Test]
        public void TestModuloDifferentFrameRateTimeAndResultIsSameFrameRate()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 45.0;
                var a = new Time(18, FrameRateA);
                var b = new Time(4, FrameRateB);
                var actual = a % b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(2));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRateA));
            });
        }

        [Test]
        public void TestModuloDifferentFrameRateTimeAndDenomination()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 30.0;
                const double FrameRateB = 21.0;
                var a = new Time(10, FrameRateA);
                var b = new Time(3, FrameRateB);
                var actual = a % b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(1));
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRateB));
            });
        }

        [Test]
        public void TestModuloDifferentFrameRateAndNonDenomination()
        {
            Assert.Multiple(() =>
            {
                const double FrameRateA = 21.0;
                const double FrameRateB = 31.0;
                const double LcdFrameRate = FrameRateA * FrameRateB;
                var a = new Time(19, FrameRateA);
                var b = new Time(1, FrameRateB);
                var actual = a % b;

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.Frame, Is.EqualTo(1));
                Assert.That(actual.FrameRate, Is.EqualTo(LcdFrameRate));
            });
        }

        [Test]
        public void TestModuloNonIntegerFrameRate()
        {
            Assert.Multiple(() =>
            {
                var FrameRateA = 30.0;
                var FrameRateB = 29.97;
                var a = new Time(6030, FrameRateA);
                var b = new Time(5994, FrameRateB);
                var actual = a % b;
                var exprectedTime = Math.Round((a.Frame / FrameRateA) % (b.Frame / FrameRateB), TimeDigit);

                // NOTE: 計算後時間は1秒になるが、30fps30フレームではなく実時間になるのは仕様通り
                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(exprectedTime));
            });
        }

        [Test]
        public void TestModuloRealTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(15, 30.0);
                var b = new Time(0.21);
                var actual = a % b;
                var expectedTimeA = Math.Round((a.Frame / FrameRate) % b.RealTime, TimeDigit);
                var expectedTimeB = Math.Round(b.RealTime % (a.Frame / FrameRate), TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That((a % b).RealTime, Is.EqualTo(expectedTimeA));
                Assert.That((b % a).RealTime, Is.EqualTo(expectedTimeB));
            });
        }

        [Test]
        public void TestModuloBothRealTime()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.32);
                var b = new Time(0.2);
                var actual = a % b;
                var expectedTime = Math.Round(a.RealTime % b.RealTime, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestModuloZero()
        {
            Assert.Throws<DivideByZeroException>(() => _ = new Time(1.0) % Time.Zero);
        }

        [Test]
        public void TestModuloeOne()
        {
            Assert.Multiple(() =>
            {
                var time = new Time(1, 30.0);

                Assert.That(time % Time.One, Is.EqualTo(Time.Zero));
                Assert.That(time % new Time(-40, 40.0), Is.EqualTo(Time.Zero));
            });
        }

        [Test]
        public void TestModuloDoubleIntegerTimeNonReminder()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(72, 30.0);
                var b = 2.0;
                var actual = a % b;
                var expectedFrame = a.Frame % ((int)b * (int)FrameRate);

                Assert.That(actual.IsFrameTime, Is.True);
                Assert.That(actual.FrameRateIsInteger, Is.True);
                Assert.That(actual.FrameRate, Is.EqualTo(FrameRate));
                Assert.That(actual.Frame, Is.EqualTo(expectedFrame));
            });
        }

        [Test]
        public void TestModuloDoubleNonIntegerTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = new Time(5, 30.0);
                var b = 0.12;
                var actual = a % b;
                var expectedTime = Math.Round((a.Frame / FrameRate) % b, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestModuloRealTimeAndDouble()
        {
            Assert.Multiple(() =>
            {
                var a = new Time(0.4);
                var b = 0.12;
                var actual = a % b;
                var expectedTime = Math.Round(a.RealTime % b, TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestModuloDoubleByTime()
        {
            Assert.Multiple(() =>
            {
                const double FrameRate = 30.0;
                var a = 0.16;
                var b = new Time(4, FrameRate);
                var actual = a % b;
                var expectedTime = Math.Round(a % (b.Frame / FrameRate), TimeDigit);

                Assert.That(actual.IsFrameTime, Is.False);
                Assert.That(actual.FrameRateIsInteger, Is.False);
                Assert.That(actual.RealTime, Is.EqualTo(expectedTime));
            });
        }

        [Test]
        public void TestModuloDoubleOne()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new Time(4, 30.0) % 1.0, Is.EqualTo(Time.Zero));
                Assert.That(new Time(-3.5) % 1.0, Is.EqualTo(Time.Zero));
            });
        }

        [Test]
        public void TestModuloDoubleZero()
        {
            Assert.Throws<DivideByZeroException>(() => _ = new Time(4, 30.0) % 0.0);
        }

        #endregion Modulo

        #region Compare

        [Test]
        public void TestLessThan()
        {
            Assert.Multiple(() =>
            {
#pragma warning disable NUnit2043 // NOTE: < のoperatorのテストのため Is.LessThanは使用しない
                Assert.That(new Time(1, 30.0) < new Time(2, 30.0), Is.True);
                Assert.That(new Time(2, 30.0) < new Time(4, 45.0), Is.True);
                Assert.That(new Time(2, 30.0) < new Time(0.1), Is.True);
                Assert.That(new Time(1.0 / 30.0) < new Time(2, 30.0), Is.True);
                Assert.That(new Time(1.0 / 30.0) < new Time(2.0 / 30.0), Is.True);

                Assert.That(new Time(1, 30.0) < new Time(1, 30.0), Is.False);
                Assert.That(new Time(1, 30.0) < new Time(2, 60.0), Is.False);
                Assert.That(new Time(3, 30.0) < new Time(0.1), Is.False);
                Assert.That(new Time(0.2) < new Time(6, 30.0), Is.False);
                Assert.That(new Time(1.0) < new Time(30 / 30.0), Is.False);

                Assert.That(new Time(2, 30.0) < new Time(1, 30.0), Is.False);
                Assert.That(new Time(3, 60.0) < new Time(1, 30.0), Is.False);
                Assert.That(new Time(3, 30.0) < new Time(0.099), Is.False);
                Assert.That(new Time(0.14) < new Time(4, 30.0), Is.False);
                Assert.That(new Time(0.11) < new Time(0.1), Is.False);
#pragma warning restore NUnit2043 // Use ComparisonConstraint for better assertion messages in case of failure
            });
        }

        [Test]
        public void TestLessThanOrEquals()
        {
            Assert.Multiple(() =>
            {
#pragma warning disable NUnit2043 // NOTE: <= のoperatorのテストのため Is.LessThanOrEqualToは使用しない
                Assert.That(new Time(1, 30.0) <= new Time(2, 30.0), Is.True);
                Assert.That(new Time(2, 30.0) <= new Time(4, 45.0), Is.True);
                Assert.That(new Time(2, 30.0) <= new Time(0.1), Is.True);
                Assert.That(new Time(1.0 / 30.0) <= new Time(2, 30.0), Is.True);
                Assert.That(new Time(1.0 / 30.0) <= new Time(2.0 / 30.0), Is.True);

                Assert.That(new Time(1, 30.0) <= new Time(1, 30.0), Is.True);
                Assert.That(new Time(1, 30.0) <= new Time(2, 60.0), Is.True);
                Assert.That(new Time(3, 30.0) <= new Time(0.1), Is.True);
                Assert.That(new Time(0.2) <= new Time(6, 30.0), Is.True);
                Assert.That(new Time(1.0) <= new Time(30 / 30.0), Is.True);

                Assert.That(new Time(2, 30.0) <= new Time(1, 30.0), Is.False);
                Assert.That(new Time(3, 60.0) <= new Time(1, 30.0), Is.False);
                Assert.That(new Time(3, 30.0) <= new Time(0.099), Is.False);
                Assert.That(new Time(0.14) <= new Time(4, 30.0), Is.False);
                Assert.That(new Time(0.11) <= new Time(0.1), Is.False);
#pragma warning restore NUnit2043 // Use ComparisonConstraint for better assertion messages in case of failure
            });
        }

        [Test]
        public void TestGreaterThan()
        {
            Assert.Multiple(() =>
            {
#pragma warning disable NUnit2043 // NOTE: > のoperatorのテストのため Is.GreaterThanは使用しない
                Assert.That(new Time(1, 30.0) > new Time(2, 30.0), Is.False);
                Assert.That(new Time(2, 30.0) > new Time(4, 45.0), Is.False);
                Assert.That(new Time(2, 30.0) > new Time(0.1), Is.False);
                Assert.That(new Time(1.0 / 30.0) > new Time(2, 30.0), Is.False);
                Assert.That(new Time(1.0 / 30.0) > new Time(2.0 / 30.0), Is.False);

                Assert.That(new Time(1, 30.0) > new Time(1, 30.0), Is.False);
                Assert.That(new Time(1, 30.0) > new Time(2, 60.0), Is.False);
                Assert.That(new Time(3, 30.0) > new Time(0.1), Is.False);
                Assert.That(new Time(0.2) > new Time(6, 30.0), Is.False);
                Assert.That(new Time(1.0) > new Time(30 / 30.0), Is.False);

                Assert.That(new Time(2, 30.0) > new Time(1, 30.0), Is.True);
                Assert.That(new Time(3, 60.0) > new Time(1, 30.0), Is.True);
                Assert.That(new Time(3, 30.0) > new Time(0.099), Is.True);
                Assert.That(new Time(0.14) > new Time(4, 30.0), Is.True);
                Assert.That(new Time(0.11) > new Time(0.1), Is.True);
#pragma warning restore NUnit2043 // Use ComparisonConstraint for better assertion messages in case of failure
            });
        }

        [Test]
        public void TestGreaterThanOrEqual()
        {
            Assert.Multiple(() =>
            {
#pragma warning disable NUnit2043 // NOTE: >= のoperatorのテストのため Is.GreaterThanOrEqualToは使用しない
                Assert.That(new Time(1, 30.0) >= new Time(2, 30.0), Is.False);
                Assert.That(new Time(2, 30.0) >= new Time(4, 45.0), Is.False);
                Assert.That(new Time(2, 30.0) >= new Time(0.1), Is.False);
                Assert.That(new Time(1.0 / 30.0) >= new Time(2, 30.0), Is.False);
                Assert.That(new Time(1.0 / 30.0) >= new Time(2.0 / 30.0), Is.False);

                Assert.That(new Time(1, 30.0) >= new Time(1, 30.0), Is.True);
                Assert.That(new Time(1, 30.0) >= new Time(2, 60.0), Is.True);
                Assert.That(new Time(3, 30.0) >= new Time(0.1), Is.True);
                Assert.That(new Time(0.2) >= new Time(6, 30.0), Is.True);
                Assert.That(new Time(1.0) >= new Time(30 / 30.0), Is.True);

                Assert.That(new Time(2, 30.0) >= new Time(1, 30.0), Is.True);
                Assert.That(new Time(3, 60.0) >= new Time(1, 30.0), Is.True);
                Assert.That(new Time(3, 30.0) >= new Time(0.099), Is.True);
                Assert.That(new Time(0.14) >= new Time(4, 30.0), Is.True);
                Assert.That(new Time(0.11) >= new Time(0.1), Is.True);
#pragma warning restore NUnit2043 // Use ComparisonConstraint for better assertion messages in case of failure
            });
        }

        [Test]
        public void TestCompareToTime()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new Time(1, 30.0).CompareTo(new Time(2, 30.0)), Is.EqualTo(-1));
                Assert.That(new Time(2, 30.0).CompareTo(new Time(1, 30.0)), Is.EqualTo(1));
                Assert.That(new Time(1, 30.0).CompareTo(new Time(1, 30.0)), Is.EqualTo(0));
                Assert.That(new Time(1, 30.0).CompareTo(new Time(2, 45.0)), Is.EqualTo(-1));
                Assert.That(new Time(2, 30.0).CompareTo(new Time(1, 45.0)), Is.EqualTo(1));
                Assert.That(new Time(2, 30.0).CompareTo(new Time(3, 45.0)), Is.EqualTo(0));
                Assert.That(new Time(1, 30.0).CompareTo(new Time(0.1)), Is.EqualTo(-1));
                Assert.That(new Time(4, 30.0).CompareTo(new Time(0.1)), Is.EqualTo(1));
                Assert.That(new Time(3, 30.0).CompareTo(new Time(0.1)), Is.EqualTo(0));
                Assert.That(new Time(0.09).CompareTo(new Time(0.1)), Is.EqualTo(-1));
                Assert.That(new Time(0.12).CompareTo(new Time(0.1)), Is.EqualTo(1));
                Assert.That(new Time(0.1).CompareTo(new Time(0.1)), Is.EqualTo(0));
            });
        }

        [Test]
        public void TestCompareToDouble()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new Time(1, 30.0).CompareTo(0.1), Is.EqualTo(-1));
                Assert.That(new Time(4, 30.0).CompareTo(0.1), Is.EqualTo(1));
                Assert.That(new Time(3, 30.0).CompareTo(0.1), Is.EqualTo(0));
            });
        }

        #endregion Compare

        #region MinMax

        [Test]
        public void TestMin()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Time.Min(new Time(1.0), new Time(29, 30.0)), Is.EqualTo(new Time(29, 30.0)));
                Assert.That(Time.IsNaN(Time.Min(Time.NaN, new Time(29, 30.0))), Is.True);
                Assert.That(Time.IsNaN(Time.Min(new Time(1.0), Time.NaN)), Is.True);
            });
        }

        [Test]
        public void TestMinNumber()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Time.MinNumber(new Time(1.0), new Time(29, 30.0)), Is.EqualTo(new Time(29, 30.0)));
                Assert.That(Time.MinNumber(Time.NaN, new Time(29, 30.0)), Is.EqualTo(new Time(29, 30.0)));
                Assert.That(Time.MinNumber(new Time(1.0), Time.NaN), Is.EqualTo(new Time(1.0)));
            });
        }

        [Test]
        public void TestMax()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Time.Max(new Time(1.0), new Time(29, 30.0)), Is.EqualTo(new Time(1.0)));
                Assert.That(Time.IsNaN(Time.Max(Time.NaN, new Time(29, 30.0))), Is.True);
                Assert.That(Time.IsNaN(Time.Max(new Time(1.0), Time.NaN)), Is.True);
            });
        }

        [Test]
        public void TestMaxNumber()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Time.MaxNumber(new Time(1.0), new Time(29, 30.0)), Is.EqualTo(new Time(1.0)));
                Assert.That(Time.MaxNumber(Time.NaN, new Time(29, 30.0)), Is.EqualTo(new Time(29, 30.0)));
                Assert.That(Time.MaxNumber(new Time(1.0), Time.NaN), Is.EqualTo(new Time(1.0)));
            });
        }

        [Test]
        public void TestClamp()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Time.Clamp(new Time(1.0), new Time(0.5), new Time(31, 30.0)), Is.EqualTo(new Time(1.0)));
                Assert.That(Time.Clamp(new Time(1.0), new Time(70, 60.0), new Time(60, 30.0)), Is.EqualTo(new Time(70, 60.0)));
                Assert.That(Time.Clamp(new Time(1.0), new Time(0.5), new Time(25, 30.0)), Is.EqualTo(new Time(25, 30.0)));
            });
        }

        [Test]
        public void TestClampMinGreaterThanMax()
        {
            Assert.Throws<ArgumentException>(() => Time.Clamp(Time.Zero, new Time(1.0), Time.Zero));
        }

        #endregion
    }
}
