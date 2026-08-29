using HarmonyLib;
using Photon.Pun;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(MonkeAgent), "IncrementRPCCallLocal")]
    public class NoIncrementRPCCallLocal
    {
        private static bool Prefix(PhotonMessageInfoWrapped infoWrapped, string rpcFunction) => false;
    }

    [HarmonyPatch(typeof(MonkeAgent), "IncrementRPCCall", new Type[] { typeof(PhotonMessageInfo), typeof(string) })]
    public class NoIncrementRPCCall
    {
        private static bool Prefix(PhotonMessageInfo info, string callingMethod = "") => false;
    }

    [HarmonyPatch(typeof(MonkeAgent), "IncrementRPCCall", new Type[] { typeof(PhotonMessageInfoWrapped), typeof(string) })]
    public class NoIncrementRPCCallWrapped
    {
        private static bool Prefix(PhotonMessageInfoWrapped infoWrapped, string callingMethod = "") => false;
    }

    [HarmonyPatch(typeof(VRRig), "IncrementRPC", new Type[] { typeof(PhotonMessageInfoWrapped), typeof(string) })]
    public class NoIncrementRPC
    {
        private static bool Prefix(PhotonMessageInfoWrapped info, string sourceCall) => false;
    }

    [HarmonyPatch(typeof(VRRig), "IncrementRPC", new Type[] { typeof(PhotonMessageInfo), typeof(string) })]
    public class NoIncrementRPCUnwrapped
    {
        private static bool Prefix(PhotonMessageInfo info, string sourceCall) => false;
    }

    [HarmonyPatch(typeof(GorillaQuitBox), nameof(GorillaQuitBox.OnBoxTriggered))]
    public class NoQuitBoxPatch
    {
        private static bool Prefix() => !mods.disableQuitbox;
    }

    [HarmonyPatch(typeof(GorillaLocomotion.GTPlayer), nameof(GorillaLocomotion.GTPlayer.SetScaleMultiplier))]
    public class SetScaleMultiplierPatch
    {
        private static bool Prefix(ref float s)
        {
            if (float.IsNaN(s) || float.IsInfinity(s) || s <= 0f)
                s = 1f;
            return true;
        }
    }

    [HarmonyPatch(typeof(GorillaTag.Audio.GTMicWrapper), "Read")]
    public class GTMicWrapperEchoPatch
    {
        private static readonly float[] delayBuffer = new float[48000];
        private static int writeHead;

        private static void Postfix(bool __result, float[] buffer)
        {
            if (!__result || !mods.microphoneEchoForOthers || buffer == null || buffer.Length == 0)
            {
                return;
            }

            int delaySamples = (int)(16000 * mods.echoDelaySeconds);
            if (delaySamples <= 0 || delaySamples >= delayBuffer.Length)
            {
                delaySamples = 4000;
            }

            float decay = Mathf.Clamp(mods.echoDecayFactor, 0.1f, 0.9f);

            for (int i = 0; i < buffer.Length; i++)
            {
                int readIndex = (writeHead - delaySamples + delayBuffer.Length) % delayBuffer.Length;
                float delayedSample = delayBuffer[readIndex];

                float mixed = buffer[i] + delayedSample * decay;
                buffer[i] = Mathf.Clamp(mixed, -1f, 1f);

                delayBuffer[writeHead] = buffer[i];
                writeHead = (writeHead + 1) % delayBuffer.Length;
            }
        }
    }

    [HarmonyPatch(typeof(GorillaTag.Audio.GTMicWrapper), "Read")]
    public class GTMicWrapperSoundboardPatch
    {
        private static void Postfix(bool __result, float[] buffer)
        {
            if (!__result || buffer == null || buffer.Length == 0) return;
            Mods.Custom.SoundboardManager.InjectMicSamples(buffer);
        }
    }

    [HarmonyPatch(typeof(GorillaTag.Audio.GTMicWrapper), "Read")]
    public class GTMicWrapperDSPPatch
    {
        private static float robotPhase;
        private static float stutterPhase;
        private static float harmonizerPhase1;
        private static float harmonizerPhase2;
        private static float cleanHp;
        private static float lowPassFilter;
        private static float radioLow;
        private static float radioHigh;

        private static void Postfix(bool __result, float[] buffer)
        {
            if (!__result || buffer == null || buffer.Length == 0) return;

            if (mods.robotMic)
            {
                const float carrier = 2f * Mathf.PI * 65f / 16000f;
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] *= Mathf.Sin(robotPhase);
                    robotPhase += carrier;
                    if (robotPhase > 2f * Mathf.PI) robotPhase -= 2f * Mathf.PI;
                }
            }

            if (mods.radioMic)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    radioLow += 0.45f * (buffer[i] - radioLow);
                    radioHigh += 0.08f * (radioLow - radioHigh);
                    float band = radioLow - radioHigh;
                    buffer[i] = Mathf.Clamp(band * 3.5f, -0.6f, 0.6f);
                }
            }

            if (mods.bitcrushMic)
            {
                const float steps = 8f;
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = Mathf.Round(buffer[i] * steps) / steps;
                }
            }

            if (mods.underwaterMic)
            {
                const float alpha = 0.12f;
                for (int i = 0; i < buffer.Length; i++)
                {
                    lowPassFilter += alpha * (buffer[i] - lowPassFilter);
                    buffer[i] = lowPassFilter * 1.5f;
                }
            }

            if (mods.stutterMic)
            {
                const float carrier = 2f * Mathf.PI * 12f / 16000f;
                for (int i = 0; i < buffer.Length; i++)
                {
                    float trem = (Mathf.Sin(stutterPhase) + 1f) * 0.5f;
                    buffer[i] *= trem;
                    stutterPhase += carrier;
                    if (stutterPhase > 2f * Mathf.PI) stutterPhase -= 2f * Mathf.PI;
                }
            }

            if (mods.harmonizerMic)
            {
                const float carrier1 = 2f * Mathf.PI * 220f / 16000f;
                const float carrier2 = 2f * Mathf.PI * 330f / 16000f;
                for (int i = 0; i < buffer.Length; i++)
                {
                    harmonizerPhase1 += carrier1;
                    harmonizerPhase2 += carrier2;
                    if (harmonizerPhase1 > 2f * Mathf.PI) harmonizerPhase1 -= 2f * Mathf.PI;
                    if (harmonizerPhase2 > 2f * Mathf.PI) harmonizerPhase2 -= 2f * Mathf.PI;
                    float harm = 0.35f * Mathf.Sin(harmonizerPhase1) + 0.25f * Mathf.Sin(harmonizerPhase2);
                    buffer[i] = Mathf.Clamp(buffer[i] * 0.7f + buffer[i] * harm * 0.6f, -1f, 1f);
                }
            }

            if (mods.cleanMic)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    cleanHp += 0.035f * (buffer[i] - cleanHp);
                    float s = buffer[i] - cleanHp;
                    float abs = Mathf.Abs(s);
                    float gain = abs > 0.4f ? 1.0f / (1.0f + (abs - 0.4f) * 2.5f) : 1.35f;
                    buffer[i] = Mathf.Clamp(s * gain, -0.95f, 0.95f);
                }
            }
        }
    }

    [HarmonyPatch(typeof(VRRig), "PlayHandTapLocal")]
    public class AntiEarrapeSoundPatch
    {
        private static readonly Dictionary<int, Queue<float>> soundTimestamps = new Dictionary<int, Queue<float>>();

        private static bool Prefix(VRRig __instance, int audioClipIndex, bool isLeftHand, float tapVolume)
        {
            if (!mods.antiEarrape || __instance == null || __instance.isLocal || __instance == VRRig.LocalRig)
                return true;

            int key = (__instance.Creator != null ? __instance.Creator.ActorNumber : __instance.GetInstanceID()) * 1000 + audioClipIndex;
            float now = Time.time;

            if (!soundTimestamps.TryGetValue(key, out var queue))
            {
                queue = new Queue<float>();
                soundTimestamps[key] = queue;
            }

            while (queue.Count > 0 && now - queue.Peek() > 1.0f)
                queue.Dequeue();

            if (queue.Count >= 10)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayFabTitleDataTextDisplay), "OnTitleDataRequestComplete")]
    public class PlayFabTitleDataTextDisplayPatch
    {
        private static void Postfix() => Main.UpdateBoardText();
    }

    [HarmonyPatch(typeof(PlayFabTitleDataTextDisplay), "OnPlayFabError")]
    public class PlayFabTitleDataErrorPatch
    {
        private static void Postfix() => Main.UpdateBoardText();
    }
}
