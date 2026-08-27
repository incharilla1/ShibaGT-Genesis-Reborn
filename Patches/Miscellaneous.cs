using HarmonyLib;
using Photon.Pun;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using System;
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
