using System.Collections;
using System.IO;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using UnityEngine;
using UnityEngine.Networking;

namespace ShibaGTGenesisReborn.Menu
{
    public static class MenuAudio
    {
        public static AudioSource audioSource;
        [Setting] public static int selectedSoundIndex = 1;
        private static readonly AudioClip[] clickClips = new AudioClip[8];
        private static bool isInitialized;

        private static string ClicksDir => Path.Combine(ModsLib.GenesisDirectory, "button_clicks");
        private static string GetAudioUrl(int index) => $"https://github.com/incharilla1/assets/raw/refs/heads/main/button_clicks/click{index}.wav";
        private static string GetAudioPath(int index) => Path.Combine(ClicksDir, $"click{index}.wav");

        public static void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            if (!Directory.Exists(ClicksDir))
                Directory.CreateDirectory(ClicksDir);

            for (int i = 1; i <= 8; i++)
            {
                int index = i;
                RunCoroutine(LoadAudio(index));
            }
        }

        private static void RunCoroutine(IEnumerator routine)
        {
            if (Main.Instance != null)
                Main.Instance.StartCoroutine(routine);
            else if (Plugin.Instance != null)
                CoroutineManager.RunCoroutine(routine);
            else if (CoroutineManager.instance != null)
                CoroutineManager.RunCoroutine(routine);
        }

        private static IEnumerator LoadAudio(int index)
        {
            string path = GetAudioPath(index);
            string url = GetAudioUrl(index);

            if (File.Exists(path))
            {
                using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    clickClips[index - 1] = DownloadHandlerAudioClip.GetContent(request);
            }
            else
            {
                using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    clickClips[index - 1] = DownloadHandlerAudioClip.GetContent(request);
                    try
                    {
                        File.WriteAllBytes(path, request.downloadHandler.data);
                    }
                    catch { }
                }
            }
        }

        public static void CycleClickSound()
        {
            selectedSoundIndex = (selectedSoundIndex % 8) + 1;
            Main.GetIndex("Cycle Button Audio").overlapText = $"Click Audio: Sound {selectedSoundIndex}";

            if (clickClips[selectedSoundIndex - 1] == null)
                RunCoroutine(LoadAndPlay(selectedSoundIndex));
            else
                PlayClickSound();
        }

        private static IEnumerator LoadAndPlay(int index)
        {
            yield return LoadAudio(index);
            PlayClickSound();
        }

        public static void PlayClickSound(float volume = 1.25f)
        {
            AudioClip clip = clickClips[selectedSoundIndex - 1];
            if (clip == null)
            {
                RunCoroutine(LoadAudio(selectedSoundIndex));
                return;
            }

            if (audioSource == null)
            {
                GameObject host = Main.Instance != null ? Main.Instance.gameObject : (GorillaTagger.Instance != null ? GorillaTagger.Instance.gameObject : null);
                if (host != null)
                {
                    audioSource = host.GetComponent<AudioSource>() ?? host.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 0f;
                }
            }

            if (audioSource != null)
                audioSource.PlayOneShot(clip, volume);
        }
    }
}
