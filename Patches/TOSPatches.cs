using System.Threading.Tasks;
using GorillaTagScripts.VirtualStumpCustomMaps.ModIO;
using HarmonyLib;
using Modio;
using Modio.Errors;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(LegalAgreements), nameof(LegalAgreements.StartLegalAgreements))]
    internal class LegalAgreementsPatch
    {
        private static bool Prefix(ref Task __result)
        {
            __result = Task.CompletedTask;
            return false;
        }
    }

    [HarmonyPatch(typeof(ModIOManager), "HasAcceptedLatestTerms")]
    internal class ModIOAcceptedTermsPatch
    {
        private static bool Prefix(ref Task<(Error, bool, bool)> __result)
        {
            __result = Task.FromResult((Error.None, true, true));
            return false;
        }
    }

    [HarmonyPatch(typeof(ModIOManager), "ShowModIOTermsOfUse")]
    internal class ModIOShowTermsPatch
    {
        private static bool Prefix(ref Task<Error> __result)
        {
            __result = Task.FromResult(Error.None);
            return false;
        }
    }

    [HarmonyPatch(typeof(ModIOTermsOfUse_v2), nameof(ModIOTermsOfUse_v2.ShowTerms))]
    internal class ModIOTermsOfUsePatch
    {
        private static bool Prefix(ref Task<Error> __result)
        {
            __result = Task.FromResult(Error.None);
            return false;
        }
    }

    [HarmonyPatch(typeof(ModIOTermsOfUse_v2), nameof(ModIOTermsOfUse_v2.StartLegalAgreements))]
    internal class ModIOStartLegalAgreementsPatch
    {
        private static bool Prefix(ref Task __result)
        {
            __result = Task.CompletedTask;
            return false;
        }
    }

    [HarmonyPatch(typeof(KIDAgeGate), nameof(KIDAgeGate.BeginAgeGate))]
    internal class AgeGatePatch
    {
        private static bool Prefix(ref Task __result)
        {
            __result = Task.CompletedTask;
            return false;
        }
    }
}
