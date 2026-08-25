using GorillaLocomotion;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace ShibaGTGenesisReborn.Mods.Custom
{
    public class SoundboardManager : MonoBehaviour
    {
        public static SoundboardManager Instance { get; private set; }
        public static float Volume = 1.0f;
        public static string CurrentTrack = "None";
        public static bool IsPlaying => _isPlaying;

        private static bool _isPlaying;
        private static float[] _pcmSamples;
        private static int _sampleRate = 44100;
        private static int _channels = 2;
        private static double _playbackPos;
        private static AudioSource _localAudio;

        public static string SoundboardDirectory => Path.Combine(ModsLib.GenesisDirectory, "soundboard");

        public static void EnsureDirectory()
        {
            if (!Directory.Exists(SoundboardDirectory))
                Directory.CreateDirectory(SoundboardDirectory);
        }

        public static void Initialize()
        {
            EnsureDirectory();
            if (Instance == null)
            {
                GameObject go = new GameObject("SoundboardManager");
                Instance = go.AddComponent<SoundboardManager>();
                _localAudio = go.AddComponent<AudioSource>();
                _localAudio.spatialBlend = 0f;
                _localAudio.volume = Volume;
                DontDestroyOnLoad(go);
            }
            RefreshSounds(false);
        }

        public static void AdjustVolume(float delta)
        {
            Volume = Mathf.Clamp(Volume + delta, 0f, 2f);
            if (_localAudio != null)
                _localAudio.volume = Volume;
            NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Soundboard Vol: {Mathf.RoundToInt(Volume * 100)}%");
        }

        public static void PlayAudioFile(string path)
        {
            if (Instance == null)
                Initialize();

            if (!File.Exists(path))
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Audio file not found");
                return;
            }

            Instance.StartCoroutine(Instance.LoadAndPlay(path));
        }

        private IEnumerator LoadAndPlay(string path)
        {
            Stop();
            string url = "file://" + path;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Failed to load audio");
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
                if (clip == null)
                {
                    NotificationLib.SendNotification(NotificationLib.NotificationType.Error, "Invalid audio clip");
                    yield break;
                }

                _sampleRate = clip.frequency;
                _channels = clip.channels;
                _pcmSamples = new float[clip.samples * clip.channels];
                clip.GetData(_pcmSamples, 0);
                _playbackPos = 0;
                _isPlaying = true;
                CurrentTrack = Path.GetFileNameWithoutExtension(path);

                if (_localAudio != null)
                {
                    _localAudio.clip = clip;
                    _localAudio.volume = Volume;
                    _localAudio.Play();
                }

                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Soundboard: {CurrentTrack}");
            }
        }

        public static void Stop()
        {
            _isPlaying = false;
            _pcmSamples = null;
            _playbackPos = 0;
            CurrentTrack = "None";
            if (_localAudio != null && _localAudio.isPlaying)
                _localAudio.Stop();
        }

        public static void InjectMicSamples(float[] buffer)
        {
            if (!_isPlaying || _pcmSamples == null || buffer == null || buffer.Length == 0)
                return;

            double step = (double)_sampleRate / 16000.0;
            for (int i = 0; i < buffer.Length; i++)
            {
                int sampleIndex = (int)_playbackPos * _channels;
                if (sampleIndex < _pcmSamples.Length)
                {
                    float sample = _pcmSamples[sampleIndex] * Volume;
                    buffer[i] = Mathf.Clamp(buffer[i] + sample, -1f, 1f);
                    _playbackPos += step;
                }
                else
                {
                    Stop();
                    break;
                }
            }
        }

        public static void OpenFolder()
        {
            EnsureDirectory();
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SoundboardDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Open soundboard folder error: {ex}");
            }
        }

        public static void RefreshSounds(bool notify = true)
        {
            EnsureDirectory();
            string[] extensions = { "*.mp3", "*.wav", "*.ogg" };
            List<string> audioFiles = new List<string>();

            foreach (string ext in extensions)
            {
                try
                {
                    audioFiles.AddRange(Directory.GetFiles(SoundboardDirectory, ext, SearchOption.TopDirectoryOnly));
                }
                catch { }
            }

            audioFiles = audioFiles.OrderBy(f => Path.GetFileName(f)).ToList();
            List<ButtonInfo> btnList = new List<ButtonInfo>
            {
                new ButtonInfo { buttonText = "Back", method = () => SettingsMods.fun(), isTogglable = false, toolTip = "Return to Fun mods" },
                new ButtonInfo { buttonText = "Refresh Audios", method = () => RefreshSounds(true), isTogglable = false, toolTip = "Rescan soundboard folder" },
                new ButtonInfo { buttonText = "Stop Soundboard", method = () => Stop(), isTogglable = false, toolTip = "Stop playing current audio" },
                new ButtonInfo { buttonText = "Open Folder", method = () => OpenFolder(), isTogglable = false, toolTip = "Open soundboard folder in Explorer" },
                new ButtonInfo { buttonText = "Volume +", method = () => AdjustVolume(0.1f), isTogglable = false, toolTip = "Increase soundboard volume" },
                new ButtonInfo { buttonText = "Volume -", method = () => AdjustVolume(-0.1f), isTogglable = false, toolTip = "Decrease soundboard volume" }
            };

            foreach (string file in audioFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string path = file;
                btnList.Add(new ButtonInfo
                {
                    buttonText = name,
                    toolTip = $"Play {name} through mic",
                    isTogglable = false,
                    method = () => PlayAudioFile(path)
                });
            }

            if (Buttons.buttons.Length > 14)
            {
                Buttons.buttons[14] = btnList.ToArray();
            }

            if (notify)
            {
                NotificationLib.SendNotification(NotificationLib.NotificationType.Info, $"Soundboard: {audioFiles.Count} audio(s) found");
                if (Main.buttonsType == 14)
                    Main.RecreateMenu();
            }
        }
    }
}
