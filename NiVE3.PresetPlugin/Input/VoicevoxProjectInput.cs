using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NiVE3.Image;
using NiVE3.Plugin.Attributes;
using NiVE3.Plugin.Interfaces;
using NiVE3.Plugin.ValueObject;
using NiVE3.PresetPlugin.Internal;
using NiVE3.PresetPlugin.Internal.Voicevox;
using NiVE3.PresetPlugin.Internal.Voicevox.Data.Project;
using NiVE3.Shared.Extension;
using NWaves.Signals;

namespace NiVE3.PresetPlugin.Input
{
    [Export(typeof(IInput))]
    [InputMetadata(typeof(VoicevoxProjectInput), "VoicevoxProjectInput", "", "mes51", ID, "*.vvproj")]
    public sealed class VoicevoxProjectInput : IInput
    {
        const string ID = "552B2B50-2C4D-4709-BC20-7DB863EBA923";

        public string FilePath { get; private set; } = "";

        Dictionary<Guid, GeneratedAudio> Audios { get; set; } = [];

        Guid[] AudioKeys { get; set; } = [];

        VoicevoxProjectData? LoadedProjectData { get; set; }

        public void SetupAccelerator(IAcceleratorObject accelerator) { }

        public FootageSourceGroup GetGroup()
        {
            var format = "D0" + (int)Math.Ceiling(Math.Log10(AudioKeys.Length));
            var sources = new List<VoicevoxAudioFootageSource>();
            foreach (var (key, i) in AudioKeys.ZipWithIndex())
            {
                if (Audios.TryGetValue(key, out var audio))
                {
                    sources.Add(new VoicevoxAudioFootageSource($"{(i + 1).ToString(format)} {audio.DisplayName}", key, audio.Wave));
                }
            }
            return new FootageSourceGroup(Path.GetFileName(FilePath), [..sources]);
        }

        public bool Load(string filePath)
        {
            var projectData = JsonSerializer.Deserialize<VoicevoxProjectData>(File.ReadAllText(filePath));
            if (projectData == null)
            {
                return false;
            }

            FilePath = filePath;
            return LoadProject(projectData);
        }

        public void Dispose() { }

        bool LoadProject(VoicevoxProjectData projectData)
        {
            var clients = VoicevoxEngineClient.GetClients();
            if (clients == null || projectData.Talk == null)
            {
                return false;
            }

            foreach (var engineId in projectData.Talk.AudioItems.Values.Select(i => i.Voice.EngineId).Distinct())
            {
                if (!clients.TryGetValue(engineId, out var client) || !client.IsAlive())
                {
                    return false;
                }
            }

            var audios = new Dictionary<Guid, GeneratedAudio>();
            foreach (var (key, item) in projectData.Talk.AudioItems)
            {
                if (string.IsNullOrEmpty(item.Text))
                {
                    continue;
                }

                var generated = clients[item.Voice.EngineId].Generate(item);
                if (generated == null)
                {
                    return false;
                }

                audios.Add(key, generated);
            }

            Audios = audios;
            AudioKeys = projectData.Talk.AudioKeys;
            LoadedProjectData = projectData;

            return true;
        }
    }

    file class VoicevoxAudioFootageSource : IFootageSource
    {
        public string SourceId => Key.ToString();

        public string? Name { get; }

        public double FrameRate { get; }

        public int Width { get; }

        public int Height { get; }

        public Time Duration => (Time)Signal.Duration;

        public SourceType SourceType => SourceType.Audio;

        Guid Key { get; }

        DiscreteSignal Signal { get; }

        public VoicevoxAudioFootageSource(string name, Guid key, DiscreteSignal signal)
        {
            Name = name;
            Key = key;
            Signal = signal;
        }

        public float[] ReadAudio(Time time, Time length)
        {
            var pos = (int)((double)time * Const.AudioSamplingRate);
            var result = new float[Math.Min((int)((double)length * Const.AudioSamplingRate), Signal.Samples.Length - pos) * 2];

            for (int i = 0, p = pos; i < result.Length; i += 2, p++)
            {
                result[i] = result[i + 1] = Signal.Samples[p];
            }

            return result;
        }

        public NImage ReadFrame(Time time, double downSamplingRate, bool toGpu)
        {
            throw new NotImplementedException();
        }
    }
}
