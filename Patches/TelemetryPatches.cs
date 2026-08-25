using HarmonyLib;
using GorillaNetworking;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Internal;
using System;
using System.Collections.Generic;

namespace ShibaGTGenesisReborn.Patches
{
    public class TelemetryPatches
    {
        [HarmonyPatch(typeof(PlayFabDeviceUtil), "SendDeviceInfoToPlayFab")]
        public class PlayfabDevicePatch1
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(PlayFabClientInstanceAPI), "ReportDeviceInfo")]
        public class PlayfabDevicePatch2
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(PlayFabClientAPI), "ReportDeviceInfo")]
        public class PlayfabDevicePatch3
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(PlayFabDeviceUtil), "GetAdvertIdFromUnity")]
        public class PlayfabDevicePatch4
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(PlayFabClientAPI), "AttributeInstall")]
        public class PlayfabDevicePatch5
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(PlayFabHttp), "InitializeScreenTimeTracker")]
        public class PlayfabDevicePatch6
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(PlayFabClientAPI), "UpdateUserTitleDisplayName")]
        public class DisplayNamePatch
        {
            public static string RandomString(int length = 4)
            {
                string random = "";
                for (int i = 0; i < length; i++)
                {
                    int rand = UnityEngine.Random.Range(0, 36);
                    char c = rand < 26
                        ? (char)('A' + rand)
                        : (char)('0' + (rand - 26));
                    random += c;
                }

                return random;
            }

            public static void Prefix(ref UpdateUserTitleDisplayNameRequest request, Action<UpdateUserTitleDisplayNameResult> resultCallback, Action<PlayFabError> errorCallback, object customData = null, Dictionary<string, string> extraHeaders = null) =>
                request.DisplayName = RandomString(UnityEngine.Random.Range(3, 12));
        }

        [HarmonyPatch(typeof(GorillaTelemetry), nameof(GorillaTelemetry.EnqueueTelemetryEvent), new Type[] { typeof(string), typeof(object), typeof(string[]) })]
        public class NoEnqueueTelemetry
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(GorillaTelemetry), "FlushMothershipTelemetry")]
        public class NoFlushTelemetry
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(GorillaServer), nameof(GorillaServer.CheckIsMothershipTelemetryEnabled))]
        public class ForceDisableMothership
        {
            private static bool Prefix(ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(CustomMapTelemetry), nameof(CustomMapTelemetry.StartMapTracking))]
        public class NoCustomMapTracking
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(CustomMapTelemetry), "EndMetricsCapture")]
        public class NoMetricsCapture
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(CustomMapTelemetry), "EndPerfCapture")]
        public class NoPerfCapture
        {
            private static bool Prefix() => false;
        }

        [HarmonyPatch(typeof(GTDev), "FetchDevID")]
        public class SpoofDevID
        {
            private static bool Prefix(ref int __result)
            {
                __result = 0;
                return false;
            }
        }
    }
}
