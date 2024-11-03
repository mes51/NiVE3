using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NiVE3.PresetPlugin.Internal.Voicevox.Data;
using NiVE3.PresetPlugin.Internal.Voicevox.Data.API;
using NiVE3.PresetPlugin.Internal.Voicevox.Data.Project;
using NWaves.Audio;
using NWaves.Operations;

namespace NiVE3.PresetPlugin.Internal.Voicevox
{
    class VoicevoxEngineClient
    {
        static private Dictionary<Guid, VoicevoxEngineClient>? Clients = null;

        public static Dictionary<Guid, VoicevoxEngineClient>? GetClients()
        {
            if (Clients != null)
            {
                return Clients;
            }

            var runtimeInfoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"voicevox\runtime-info.json");
            if (!File.Exists(runtimeInfoPath))
            {
                return null;
            }

            var runtimeInfo = JsonSerializer.Deserialize<RuntimeInfoData>(File.ReadAllText(runtimeInfoPath));
            if (runtimeInfo == null)
            {
                return null;
            }

            Clients = runtimeInfo.EngineInfos.ToDictionary(e => e.Uuid, e => new VoicevoxEngineClient(e.Url));

            return Clients;
        }

        HttpClient HttpClient { get; }

        SpeakerData[]? Speakers { get; set; }

        private VoicevoxEngineClient(string url)
        {
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };
        }

        public bool IsAlive()
        {
            try
            {
                var version = HttpClient.GetAsync("/version");
                version.Wait();
                return version.IsCompleted;
            }
            catch
            {
                return false;
            }
        }

        public GeneratedAudio? Generate(ProjectAudioItemData audioItemData)
        {
            UpdateSpeakers();
            if (Speakers == null || audioItemData.Query == null)
            {
                return null;
            }

            var query = ConvertQuery(audioItemData.Query);
            var path = $"/synthesis?speaker={audioItemData.Voice.StyleId}";
            if (audioItemData.MorphingInfo != null)
            {
                path = $"/synthesis_morphing?base_speaker={audioItemData.Voice.StyleId}&target_speaker={audioItemData.MorphingInfo.TargetStyleId}&morph_rate={audioItemData.MorphingInfo.Rate}";
            }

            try
            {
                var result = HttpClient.PostAsJsonAsync(path, query);
                result.Wait();
                if (result.IsFaulted || result.Result.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                using var stream = result.Result.Content.ReadAsStream();
                var wave = new WaveFile(stream);

                var signal = wave[Channels.Average];
                if (wave.WaveFmt.SamplingRate != Const.AudioSamplingRate)
                {
                    signal = Operation.Resample(signal, Const.AudioSamplingRate);
                }

                var speaker = Speakers.First(s => s.SpeakerUuid == audioItemData.Voice.SpeakerId);
                var speakerName = speaker.Styles.Length > 1 ? $"{speaker.Name} {speaker.Styles.First(s => s.Id == audioItemData.Voice.StyleId).Name}" : speaker.Name;
                return new GeneratedAudio(signal, speakerName, audioItemData.Text);
            }
            catch
            {
                return null;
            }
        }

        void UpdateSpeakers()
        {
            if (Speakers != null)
            {
                return;
            }

            try
            {
                var speakerResult = HttpClient.GetAsync("/speakers");
                speakerResult.Wait();
                if (speakerResult.IsFaulted || speakerResult.Result.StatusCode != HttpStatusCode.OK)
                {
                    return;
                }

                using var stream = speakerResult.Result.Content.ReadAsStream();
                Speakers = JsonSerializer.Deserialize<SpeakerData[]>(stream);
            }
            catch { }
        }

        static APIAudioQueryData ConvertQuery(ProjectAudioQueryData audioQueryData)
        {
            return new APIAudioQueryData
            {
                AccentPhases = [..audioQueryData.AccentPhrases.Select(a => new APIAccentPhaseData
                {
                    Moras = [..a.Moras.Select(m => new APIMoraData
                    {
                        Text = m.Text,
                        Vowel = m.Vowel,
                        VowelLength = m.VowelLength,
                        Pitch = m.Pitch,
                        Consonant = m.Consonant,
                        ConsonantLength = m.ConsonantLength,
                    })],
                    Accent = a.Accent,
                    PauseMora = a.PauseMora != null ? new APIMoraData
                    {
                        Text = a.PauseMora.Text,
                        Vowel = a.PauseMora.Vowel,
                        VowelLength = a.PauseMora.VowelLength,
                        Pitch = a.PauseMora.Pitch,
                        Consonant = a.PauseMora.Consonant,
                        ConsonantLength = a.PauseMora.ConsonantLength,
                    } : null,
                    IsInterrogative = a.isInterrogative ?? false,
                })],
                SpeedScale = audioQueryData.SpeedScale,
                PitchScale = audioQueryData.PitchScale,
                IntonationScale = audioQueryData.IntonationScale,
                VolumeScale = audioQueryData.VolumeScale,
                PrePhonemeLength = audioQueryData.PrePhonemeLength,
                PostPhonemeLength = audioQueryData.PostPhonemeLength,
                OutputSamplingRate = audioQueryData.OutputSamplingRate,
                OutputStereo = audioQueryData.OutputStereo,
                Kana = audioQueryData.Kana
            };
        }

        ~VoicevoxEngineClient()
        {
            HttpClient.Dispose();
        }
    }
}
