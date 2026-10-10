using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShibaGTGenesisReborn.Patches
{
    [HarmonyPatch(typeof(PhotonNetwork), "RPC", new Type[] { typeof(PhotonView), typeof(string), typeof(RpcTarget), typeof(Player), typeof(bool), typeof(object[]) })]
    public class RpcLoggerPatch
    {
        public static bool Enabled = true;

        private static void Prefix(PhotonView view, string methodName, RpcTarget target, Player player, bool encrypt, object[] parameters)
        {
            if (!Enabled) return;

            try
            {
                List<string> parts = new List<string>();

                if (!string.IsNullOrEmpty(methodName)) parts.Add(methodName);

                string targetStr = player != null ? $"Player #{player.ActorNumber} ({player.NickName})" : target.ToString();
                if (!string.IsNullOrEmpty(targetStr)) parts.Add($"Target: {targetStr}");

                if (view != null)
                {
                    string goName = view.gameObject != null ? view.gameObject.name : null;
                    parts.Add(!string.IsNullOrEmpty(goName) ? $"ViewID: {view.ViewID} ({goName})" : $"ViewID: {view.ViewID}");
                }

                if (encrypt)
                    parts.Add("Encrypt: True");

                if (parameters != null && parameters.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        object p = parameters[i];
                        if (p == null) sb.Append("null");
                        else if (p is object[] arr) sb.Append($"[{string.Join(", ", arr)}]");
                        else sb.Append(p.ToString());
                    }
                    parts.Add($"Params: {sb}");
                }

                Console.WriteLine(string.Join(" | ", parts));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
    }
}
