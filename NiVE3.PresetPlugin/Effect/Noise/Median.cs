using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using ComputeSharp;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.Property;
using NiVE3.Plugin.Property.Properties;
using NiVE3.Plugin.Resource;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Effect.Util;
using NiVE3.PresetPlugin.Extension;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Resource;

namespace NiVE3.PresetPlugin.Effect.Noise
{
    [EffectMetadata(LanguageResourceDictionary.Noise_Median_Name, "mes51", DefaultLanguageResourceNames.EffectCategory_Noise, LanguageResourceDictionary.Noise_Median_Description, ID, LanguageResourceDictionaryType = typeof(LanguageResourceDictionary))]
    [Export(typeof(IEffect))]
    public sealed class Median : IEffect
    {
        const string ID = "DD64884D-BAA1-40C2-AB60-96F19604655A";

        const string PropertyRadiusId = nameof(PropertyRadiusId);

        const string PropertyApplyToAlphaId = nameof(PropertyApplyToAlphaId);

        IAcceleratorObject? AcceleratorObject { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator)
        {
            AcceleratorObject = accelerator;
        }

        public PropertyBase[] GetProperties(Int32Size sourceSize)
        {
            return
            [
                new DoubleProperty(PropertyRadiusId, LanguageResourceDictionary.ResourceKeys.Noise_Median_Radius, 0.0, 0.0, 300.0, digit: 0),
                new CheckBoxProperty(PropertyApplyToAlphaId, LanguageResourceDictionary.ResourceKeys.Noise_Median_ApplyToAlpha, false)
            ];
        }

        public NImage Process(NImage image, ROI roi, double downSamplingRateX, double downSamplingRateY, Time layerTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer, bool useGpu)
        {
            var radius = (int)properties.GetValue(PropertyRadiusId, layerTime, 0.0);
            var applyToAlpha = (bool)properties.GetValue(PropertyApplyToAlphaId, layerTime, false);
            if (radius < 1)
            {
                return image;
            }

            return ProcessCpu(image, roi, radius, applyToAlpha);
        }

        public float[] Process(float[] audio, Time startTime, IPropertyObject[] properties, ICompositionObject composition, ILayerObject layer)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        static NManagedImage ProcessCpu(NImage image, ROI roi, int radius, bool applyToAlpha)
        {
            var managedImage = image.ToManaged();

            var imageData = managedImage.Data;
            var imageWidth = managedImage.Width;
            var imageHeight = managedImage.Height;
            var k = radius * 2 + 1;
            var temp = ArrayPool<Vector4>.Shared.Rent(managedImage.DataLength);
            temp.AsSpan(0, managedImage.DataLength).Clear();

            // NOTE: 本来medianはSeparableではないが、効果としては大体同じになるので縦横で分割する
            Parallel.For(Math.Max(roi.Top - radius, 0), Math.Min(roi.Bottom + radius, imageHeight), y =>
            {
                var imageDataSpan = imageData.AsSpan(y * imageWidth, imageWidth);
                var linkNodes = Enumerable.Range(0, k).Select(_ => new LinkedListNode<float>(0.0F)).ToArray();
                var list = new LinkedList<float>();
                var channelCount = applyToAlpha ? 4 : 3;
                var medianPos = k / 2 - (k % 2 == 1 ? 0 : 1);

                for (var c = 0; c < channelCount; c++)
                {
                    list.Clear();

                    for (var i = 0; i < linkNodes.Length; i++)
                    {
                        linkNodes[i].Value = imageDataSpan[CoordWrap.Mirror(roi.Left + i - radius - 2, imageWidth)][c];
                        InsertToSortedList(list, linkNodes[i], null);
                    }

                    var medianNode = GetNode(list, medianPos);

                    for (var x = roi.Left; x < roi.Right; x++)
                    {
                        var node = linkNodes[CoordWrap.Repeat(x - radius - 1, k)];
                        var removePrevNode = false;
                        if (node == medianNode)
                        {
                            medianNode = medianNode.Next;
                        }
                        else
                        {
                            var prevNode = medianNode.Previous;
                            while (prevNode != null)
                            {
                                if (prevNode == node)
                                {
                                    removePrevNode = true;
                                    break;
                                }
                                else
                                {
                                    prevNode = prevNode.Previous;
                                }
                            }
                        }
                        if (medianNode == null)
                        {
                            throw new Exception("median is not found"); // bug
                        }

                        list.Remove(node);
                        node.Value = imageDataSpan[CoordWrap.Mirror(x + radius, imageWidth)][c];
                        InsertToSortedList(list, node, medianNode);

                        if (removePrevNode)
                        {
                            if (node.Value >= medianNode.Value)
                            {
                                medianNode = medianNode.Next;
                            }
                        }
                        else
                        {
                            if (node.Value < medianNode.Value)
                            {
                                medianNode = medianNode.Previous;
                            }
                        }
                        if (medianNode == null)
                        {
                            throw new Exception("median is not found"); // bug
                        }

                        ref var pixel = ref Unsafe.Add(ref temp[x * imageHeight + y].X, c);
                        pixel = medianNode.Value;
                    }
                }
            });

            Parallel.For(roi.Left, roi.Right, x =>
            {
                var tempSpan = temp.AsSpan(x * imageHeight, imageHeight);
                var linkNodes = Enumerable.Range(0, k).Select(_ => new LinkedListNode<float>(0.0F)).ToArray();
                var list = new LinkedList<float>();
                var channelCount = applyToAlpha ? 4 : 3;
                var medianPos = k / 2 - (k % 2 == 1 ? 0 : 1);

                for (var c = 0; c < channelCount; c++)
                {
                    list.Clear();

                    for (var i = 0; i < linkNodes.Length; i++)
                    {
                        linkNodes[i].Value = tempSpan[CoordWrap.Mirror(roi.Top + i - radius - 2, imageHeight)][c];
                        InsertToSortedList(list, linkNodes[i], null);
                    }

                    var medianNode = GetNode(list, medianPos);

                    for (var y = roi.Top; y < roi.Bottom; y++)
                    {
                        var node = linkNodes[CoordWrap.Repeat(y - radius - 1, k)];
                        var removePrevNode = false;
                        if (node == medianNode)
                        {
                            medianNode = medianNode.Next;
                        }
                        else
                        {
                            var prevNode = medianNode.Previous;
                            while (prevNode != null)
                            {
                                if (prevNode == node)
                                {
                                    removePrevNode = true;
                                    break;
                                }
                                else
                                {
                                    prevNode = prevNode.Previous;
                                }
                            }
                        }
                        if (medianNode == null)
                        {
                            throw new Exception("median is not found"); // bug
                        }

                        list.Remove(node);
                        node.Value = tempSpan[CoordWrap.Mirror(y + radius, imageHeight)][c];
                        InsertToSortedList(list, node, medianNode);

                        if (removePrevNode)
                        {
                            if (node.Value >= medianNode.Value)
                            {
                                medianNode = medianNode.Next;
                            }
                        }
                        else
                        {
                            if (node.Value < medianNode.Value)
                            {
                                medianNode = medianNode.Previous;
                            }
                        }
                        if (medianNode == null)
                        {
                            throw new Exception("median is not found"); // bug
                        }

                        ref var pixel = ref Unsafe.Add(ref imageData[y * imageWidth + x].X, c);
                        pixel = medianNode.Value;
                    }
                }
            });

            ArrayPool<Vector4>.Shared.Return(temp);

            return managedImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InsertToSortedList<T>(LinkedList<T> list, LinkedListNode<T> node, LinkedListNode<T>? medianNode) where T : IComparisonOperators<T, T, bool>
        {
            var currentNode = medianNode != null && node.Value >= medianNode.Value ? medianNode : list.First;
            while (currentNode != null)
            {
                if (currentNode.Value <= node.Value)
                {
                    currentNode = currentNode.Next;
                }
                else
                {
                    list.AddBefore(currentNode, node);
                    return;
                }
            }
            if (currentNode == null)
            {
                list.AddLast(node);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static LinkedListNode<T> GetNode<T>(LinkedList<T> list, int index)
        {
            var node = list.First;
            for (var i = 1; i <= index; i++)
            {
                node = node?.Next;
            }

            if (node == null)
            {
                throw new IndexOutOfRangeException();
            }

            return node;
        }
    }
}
