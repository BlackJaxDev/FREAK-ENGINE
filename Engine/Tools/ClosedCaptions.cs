using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using NAudio.Wave;
using Google.Cloud.Speech.V1;
using OscCore;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace XREngine.Tools
{
    public class ClosedCaptions
    {
        static ConcurrentQueue<(byte[], bool)> _audioQueue = new ConcurrentQueue<(byte[], bool)>();

        static SpeechRecognizer recognizer;
        static JObject config = new JObject
        {
            ["FollowMicMute"] = true,
            ["CapturedLanguage"] = "en-US",
            ["TranscriptionMethod"] = "Google",
            ["TranscriptionRateLimit"] = 1200,
            ["EnableTranslation"] = false,
            ["TranslateMethod"] = "Google",
            ["TranslateToken"] = "",
            ["TranslateTo"] = "en-US",
            ["AllowOSCControl"] = true,
            ["Pause"] = false,
            ["TranslateInterumResults"] = true,
            ["OSCControlPort"] = 9001
        };
        static JObject state = new JObject
        {
            ["selfMuted"] = false
        };
        static object stateLock = new object();

        // STATE MANAGEMENT
        // This should be thread-safe
        static object GetState(string key)
        {
            lock (stateLock)
            {
                return state[key];
            }
        }

        static void SetState(string key, object value)
        {
            lock (stateLock)
            {
                state[key] = JToken.FromObject(value);
            }
        }

        // SOUND PROCESSING THREAD
        static void ProcessSound()
        {
            // Note: You will need to find an equivalent C# library for OSC communication.
            // The following is an example using OscCore, which can be found at https://github.com/stella3d/OscCore
            var client = new OscClient(IPAddress.Parse("127.0.0.1"), 9000);

            string currentText = "";
            string lastText = "";
            DateTime lastDispTime = DateTime.Now;
            // Note: You will need to find an equivalent C# library for translating text.
            object translator = null;

            Console.WriteLine("[ProcessThread] Starting audio processing!");
            while (true)
            {
                // Handle translation setup and related logic here.

                _audioQueue.TryDequeue(out (byte[], bool) audioDataTuple);

                var (ad, final) = audioDataTuple;

                if (config.Value<bool>("FollowMicMute") && (bool)GetState("selfMuted"))
                {
                    continue;
                }

                if (config.Value<bool>("Pause"))
                {
                    continue;
                }

                // Handle OSC communication here.

                if (config.Value<bool>("EnableTranslation") && !config.Value<bool>("TranslateInterumResults") && !final)
                {
                    continue;
                }

                string text = null;

                // Handle transcription logic here.

                if (text == null || text == "")
                {
                    continue;
                }

                currentText = text;

                if (lastText == currentText)
                {
                    continue;
                }

                lastText = currentText;

                TimeSpan difference = DateTime.Now - lastDispTime;
                int diffInMilliseconds = (int)difference.TotalMilliseconds;
                if (diffInMilliseconds < config.Value<int>("TranscriptionRateLimit"))
                {
                    int msToSleep = config.Value<int>("TranscriptionRateLimit") - diffInMilliseconds;
                    Console.WriteLine($"[ProcessThread] Sending too many messages! Delaying by {(msToSleep / 1000.0)} sec to not hit rate limit!");
                    Thread.Sleep(msToSleep);
                }

                // Handle translation and related logic here.

                if (text.Length > 144)
                {
                    currentText = text.Substring(Math.Max(0, text.Length - 144));
                }

                lastDispTime = DateTime.Now;

                // Handle OSC communication here.

            }
        }

        // AUDIO COLLECTION THREAD
        static void CollectAudio()
        {
            Console.WriteLine("[AudioThread] Starting audio collection!");

            int waveInDevices = WaveIn.DeviceCount;
            for (int i = 0; i < waveInDevices; i++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(i);
                Console.WriteLine($"Device {i}: {deviceInfo.ProductName}, {deviceInfo.Channels} channels");
            }

            WaveInEvent waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 1)
            };

            waveIn.DataAvailable += (sender, e) =>
            {
                byte[] audioData = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, audioData, e.BytesRecorded);
                _audioQueue.Enqueue((audioData, false));
            };

            waveIn.StartRecording();
            Console.WriteLine("[AudioThread] Recording started.");

            Thread.Sleep(Timeout.Infinite);

            waveIn.StopRecording();
            Console.WriteLine("[AudioThread] Recording stopped.");
        }

        // MAIN ROUTINE
        public static void Run()
        {
            // Load config
            string cfgFile = $"{Path.GetDirectoryName(Path.GetFullPath(Environment.CurrentDirectory))}/Config.yml";
            if (File.Exists(cfgFile))
            {
                Console.WriteLine($"Loading config from {cfgFile}");
                // Deserialize the YAML config file to a JObject and update the config JObject here.
            }

            // Start threads
            Thread pst = new Thread(ProcessSound);
            pst.Start();

            Thread cat = new Thread(CollectAudio);
            cat.Start();

            // Handle OSC server logic here.

            pst.Join();
            cat.Join();

            // If using OSC, handle OSC server shutdown here.
        }
    }
}