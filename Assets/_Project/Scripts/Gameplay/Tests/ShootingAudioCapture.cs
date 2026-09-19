#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Unomata.EditorValidation
{
    // Transient Editor validation component; never saved into a scene or prefab.
    public sealed class ShootingAudioCapture : MonoBehaviour
    {
        private static ShootingAudioCapture _active;
        private readonly object _gate = new object();
        private float[] _buffer;
        private int _count, _channels, _sampleRate;
        private volatile bool _capturing;

        public static string Begin()
        {
            if (!Application.isPlaying || Camera.main == null) throw new InvalidOperationException("Requires a playing scene with the main audio listener.");
            if (_active != null) throw new InvalidOperationException("Audio recording already active.");
            _active = Camera.main.gameObject.AddComponent<ShootingAudioCapture>();
            _active.hideFlags = HideFlags.DontSave;
            _active._sampleRate = AudioSettings.outputSampleRate;
            _active._buffer = new float[_active._sampleRate * 8 * 30];
            _active._capturing = true;
            return "Recording game audio only.";
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_capturing) return;
            lock (_gate)
            {
                if (!_capturing || _buffer == null) return;
                _channels = channels;
                int length = Math.Min(data.Length, _buffer.Length - _count);
                Array.Copy(data, 0, _buffer, _count, length); _count += length;
            }
        }

        public static string Finish()
        {
            if (_active == null) return "No active audio capture.";
            var recorder = _active; _active = null; recorder._capturing = false;
            float peak = 0; double energy = 0; int clipped = 0;
            lock (recorder._gate)
            {
                Directory.CreateDirectory(".utmp/shooting");
                if (recorder._channels > 0 && recorder._count > 0)
                using (var writer = new BinaryWriter(File.Create(".utmp/shooting/game-audio.wav")))
                {
                    int bytes = recorder._count * 2;
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                    writer.Write((short)1); writer.Write((short)recorder._channels); writer.Write(recorder._sampleRate);
                    writer.Write(recorder._sampleRate * recorder._channels * 2); writer.Write((short)(recorder._channels * 2)); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
                    for (int i = 0; i < recorder._count; i++)
                    {
                        float value = recorder._buffer[i]; peak = Math.Max(peak, Math.Abs(value)); energy += (double)value * value;
                        if (Math.Abs(value) > 1) clipped++;
                        writer.Write((short)(Math.Max(-1, Math.Min(1, value)) * short.MaxValue));
                    }
                }
                var report = new
                {
                    sampleRate = recorder._sampleRate, channels = recorder._channels, samples = recorder._count,
                    seconds = recorder._channels == 0 ? 0 : (double)recorder._count / recorder._channels / recorder._sampleRate,
                    peak, rms = recorder._count == 0 ? 0 : Math.Sqrt(energy / recorder._count), clippedSamples = clipped
                };
                string json = JsonConvert.SerializeObject(report, Formatting.Indented);
                File.WriteAllText(".utmp/shooting/audio-capture.json", json);
                Destroy(recorder);
                return json;
            }
        }
        private void OnDestroy() { _capturing = false; if (_active == this) _active = null; }
    }
}

#endif
