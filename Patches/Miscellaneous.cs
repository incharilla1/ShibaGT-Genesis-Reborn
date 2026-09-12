using HarmonyLib;
using Photon.Pun;
using ShibaGTGenesisReborn.Menu;
using ShibaGTGenesisReborn.Mods;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    [HarmonyPatch(typeof(GorillaTag.Audio.GTRecorder), "PostTick")]
    public class GTRecorderEffectsPatch
    {
        private static GorillaTag.Audio.GTRecorder recorder;
        private static Photon.Voice.Unity.Recorder.MicType micType;
        private static bool fallback;
        private static bool detection;

        private static void Prefix(GorillaTag.Audio.GTRecorder __instance)
        {
            if (__instance != mods.GetActiveGTRecorder()) return;
            bool active = __instance.AllowPitchAdjustment || __instance.AllowVolumeAdjustment ||
                mods.microphoneEchoForOthers || mods.robotMic || mods.radioMic || mods.bitcrushMic ||
                mods.underwaterMic || mods.stutterMic || mods.harmonizerMic || mods.cleanMic ||
                Mods.Custom.SoundboardManager.IsPlaying;
            if (recorder != null && (recorder != __instance || !active))
            {
                recorder.MicrophoneType = micType;
                recorder.UseMicrophoneTypeFallback = fallback;
                recorder.VoiceDetection = detection;
                recorder = null;
            }
            if (!active) return;
            if (recorder == null)
            {
                recorder = __instance;
                micType = recorder.MicrophoneType;
                fallback = recorder.UseMicrophoneTypeFallback;
                detection = recorder.VoiceDetection;
                recorder.UseMicrophoneTypeFallback = false;
                if (recorder.MicrophoneType == Photon.Voice.Unity.Recorder.MicType.Unity)
                    recorder.RestartRecording(true);
            }
            recorder.VoiceDetection = false;
            recorder.UseMicrophoneTypeFallback = false;
            if (recorder.MicrophoneType != Photon.Voice.Unity.Recorder.MicType.Unity)
                recorder.MicrophoneType = Photon.Voice.Unity.Recorder.MicType.Unity;
        }
    }

    [HarmonyPatch(typeof(GorillaTag.Audio.GTMicWrapper), "Read")]
    public class GTMicWrapperEchoPatch
    {
        private sealed class EchoState
        {
            public float[] Buffer;
            public int Head;
        }

        private static readonly ConditionalWeakTable<GorillaTag.Audio.GTMicWrapper, EchoState> states = new ConditionalWeakTable<GorillaTag.Audio.GTMicWrapper, EchoState>();

        private static void Postfix(GorillaTag.Audio.GTMicWrapper __instance, bool __result, float[] buffer)
        {
            if (!mods.microphoneEchoForOthers)
            {
                states.Remove(__instance);
                return;
            }
            if (!__result || buffer == null || buffer.Length == 0 || __instance.SamplingRate <= 0 || __instance.Channels <= 0) return;

            EchoState state = states.GetValue(__instance, mic => new EchoState());
            int delaySamples = Mathf.Max(1, (int)(__instance.SamplingRate * Mathf.Clamp(mods.echoDelaySeconds, 0.01f, 2f))) * __instance.Channels;
            if (state.Buffer == null || state.Buffer.Length != delaySamples)
            {
                state.Buffer = new float[delaySamples];
                state.Head = 0;
            }

            float decay = Mathf.Clamp(mods.echoDecayFactor, 0.1f, 0.9f);

            for (int i = 0; i < buffer.Length; i++)
            {
                float mixed = buffer[i] + state.Buffer[state.Head] * decay;
                buffer[i] = Mathf.Clamp(mixed, -1f, 1f);

                state.Buffer[state.Head] = buffer[i];
                state.Head = (state.Head + 1) % delaySamples;
            }
        }
    }

    [HarmonyPatch(typeof(GorillaTag.Audio.GTMicWrapper), "Read")]
    public class GTMicWrapperSoundboardPatch
    {
        private static bool Prefix(GorillaTag.Audio.GTMicWrapper __instance, float[] buffer, ref bool __result)
        {
            if (!Mods.Custom.SoundboardManager.IsPlaying || buffer == null || buffer.Length == 0)
                return true;

            int micSamplingRate = ((Photon.Voice.Unity.MicWrapper)__instance).SamplingRate;
            if (micSamplingRate <= 0) micSamplingRate = 16000;

            double elapsed = (double)Time.realtimeSinceStartup - Mods.Custom.SoundboardManager.StartTime;
            long availableSamples = (long)(elapsed * micSamplingRate);
            long neededSamples = Mods.Custom.SoundboardManager.SamplesSent + buffer.Length;

            if (neededSamples > availableSamples)
            {
                __result = false;
                return false;
            }

            Array.Clear(buffer, 0, buffer.Length);
            bool ok = Mods.Custom.SoundboardManager.FillBuffer(buffer, micSamplingRate);
            __result = ok;
            return false;
        }
    }

    [HarmonyPatch(typeof(GorillaTag.Audio.GTMicWrapper), "Read")]
    public class GTMicWrapperDSPPatch
    {
        private static readonly ConditionalWeakTable<GorillaTag.Audio.GTMicWrapper, DspState[]> states = new ConditionalWeakTable<GorillaTag.Audio.GTMicWrapper, DspState[]>();

        private static void Postfix(GorillaTag.Audio.GTMicWrapper __instance, bool __result, float[] buffer)
        {
            if (!__result || buffer == null || buffer.Length == 0 || __instance.SamplingRate <= 0 || __instance.Channels <= 0) return;
            DspState[] channels = states.GetValue(__instance, mic =>
            {
                DspState[] result = new DspState[mic.Channels];
                for (int i = 0; i < result.Length; i++) result[i] = new DspState();
                return result;
            });
            for (int i = 0; i < channels.Length; i++)
                channels[i].Process(buffer, __instance.SamplingRate, i, channels.Length);
        }

        private sealed class DspState
        {
            private float robotPhase;
            private float stutterPhase;
            private float harmonizerPhase1;
            private float harmonizerPhase2;
            private float cleanHp;
            private float cleanEnvelope;
            private float lowPassFilter;
            private float radioLow;
            private float radioHigh;

            public void Process(float[] buffer, int rate, int channel, int channels)
            {
                if (mods.robotMic)
                {
                    float carrier = 2f * Mathf.PI * 65f / rate;
                    for (int i = channel; i < buffer.Length; i += channels)
                    {
                        buffer[i] *= Mathf.Sin(robotPhase);
                        robotPhase += carrier;
                        if (robotPhase > 2f * Mathf.PI) robotPhase -= 2f * Mathf.PI;
                    }
                }

                if (mods.radioMic)
                {
                    float low = 1f - Mathf.Exp(-2f * Mathf.PI * 3400f / rate);
                    float high = 1f - Mathf.Exp(-2f * Mathf.PI * 300f / rate);
                    for (int i = channel; i < buffer.Length; i += channels)
                    {
                        radioLow += low * (buffer[i] - radioLow);
                        radioHigh += high * (radioLow - radioHigh);
                        float band = radioLow - radioHigh;
                        buffer[i] = Mathf.Clamp(band * 3.5f, -0.6f, 0.6f);
                    }
                }

                if (mods.bitcrushMic)
                {
                    const float steps = 128f;
                    for (int i = channel; i < buffer.Length; i += channels)
                    {
                        buffer[i] = Mathf.Round(buffer[i] * steps) / steps;
                    }
                }

                if (mods.underwaterMic)
                {
                    float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * 325f / rate);
                    for (int i = channel; i < buffer.Length; i += channels)
                    {
                        lowPassFilter += alpha * (buffer[i] - lowPassFilter);
                        buffer[i] = lowPassFilter * 1.5f;
                    }
                }

                if (mods.stutterMic)
                {
                    float carrier = 2f * Mathf.PI * 12f / rate;
                    for (int i = channel; i < buffer.Length; i += channels)
                    {
                        float trem = (Mathf.Sin(stutterPhase) + 1f) * 0.5f;
                        buffer[i] *= trem;
                        stutterPhase += carrier;
                        if (stutterPhase > 2f * Mathf.PI) stutterPhase -= 2f * Mathf.PI;
                    }
                }

                if (mods.harmonizerMic)
                {
                    float carrier1 = 2f * Mathf.PI * 220f / rate;
                    float carrier2 = 2f * Mathf.PI * 330f / rate;
                    for (int i = channel; i < buffer.Length; i += channels)
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
                    float high = 1f - Mathf.Exp(-2f * Mathf.PI * 90f / rate);
                    float release = Mathf.Exp(-1f / (rate * 0.05f));
                    for (int i = channel; i < buffer.Length; i += channels)
                    {
                        cleanHp += high * (buffer[i] - cleanHp);
                        float s = buffer[i] - cleanHp;
                        float abs = Mathf.Abs(s);
                        cleanEnvelope = Mathf.Max(abs, cleanEnvelope * release);
                        float gate = Mathf.Clamp01((cleanEnvelope - 0.001f) / 0.003f);
                        float gain = gate * (abs > 0.4f ? 1.0f / (1.0f + (abs - 0.4f) * 2.5f) : 1.35f);
                        buffer[i] = Mathf.Clamp(s * gain, -0.95f, 0.95f);
                    }
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
