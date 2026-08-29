using ExitGames.Client.Photon;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        public static void TagGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null &&
                    !GunLib.LockedPlayer.mainSkin.material.name.Contains("fected") &&
                    !GunLib.LockedPlayer.isLocal)
                {
                    if (VRRig.LocalRig.mainSkin.material.name.Contains("fected"))
                    {
                        TagPlayer(GunLib.LockedPlayer);
                    }
                    
                }
            }, true);
        }

        public static void TagPlayer(VRRig p)
        {
            if (p == null || VRRig.LocalRig == null) return;
            if (!p.mainSkin.material.name.Contains("fected") && VRRig.LocalRig.mainSkin.material.name.Contains("fected"))
            {
                bypasstp(p.bodyTransform.position, true);
                GameMode.ReportTag(RigManager.GetPlayerFromVRRig(p));
            }
        }

        public static void TagAll()
        {
            foreach (VRRig p in VRRigCache.ActiveRigs)
            {
                if (!p.isLocal)
                {
                    TagPlayer(p);
                }
            }
        }

        public static void TagSelf()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            if (VRRig.LocalRig != null && !VRRig.LocalRig.mainSkin.material.name.Contains("fected"))
            {
                foreach (VRRig rig in VRRigCache.ActiveRigs)
                {
                    if (rig != null && !rig.isLocal && rig.mainSkin.material.name.Contains("fected"))
                    {
                        bypasstp(rig.bodyTransform.position, true);
                        break;
                    }
                }

                if (PhotonNetwork.LocalPlayer != null)
                {
                    GameMode.ReportTag(PhotonNetwork.LocalPlayer);
                }
            }
        }

        public static void NoTagOnJoin()
        {
            PlayerPrefs.SetString("didTutorial", "nope");
            PlayerPrefs.SetString("tutorial", "nope");
            Hashtable hasht = new Hashtable();
            hasht.Add("didTutorial", false);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hasht, null, null);
            PlayerPrefs.Save();
        }

        public static string _leavesName;
        public static readonly List<GameObject> leaves = new List<GameObject>();
        private static readonly Dictionary<string, GameObject> objectPool = new Dictionary<string, GameObject>();
        public static void removeleaves()
        {
            if (_leavesName == null)
            {
                var path = "Environment Objects/LocalObjects_Prefab/Forest";
                if (!objectPool.TryGetValue(path, out var f))
                {
                    f = GameObject.Find(path);
                    if (f != null)
                        objectPool.Add(path, f);
                }

                if (f != null)
                {
                    var counts = new Dictionary<string, (int count, int siblingIndex)>();
                    for (int i = 0; i < f.transform.childCount; i++)
                    {
                        var t = f.transform.GetChild(i);
                        if (!t.name.StartsWith("UnityTempFile"))
                            continue;
                        if (!counts.TryGetValue(t.name, out var entry))
                            counts[t.name] = (1, t.GetSiblingIndex());
                        else
                            counts[t.name] = (entry.count + 1, entry.siblingIndex);
                    }
                    _leavesName = counts.Where(kv => kv.Value.count == 3).OrderByDescending(kv => kv.Value.siblingIndex).FirstOrDefault().Key ?? "UnityTempFile";
                }
            }

            foreach (var path in new[] { "Environment Objects/LocalObjects_Prefab/Forest", "RankedMain/Ranked_Layout/Ranked_Forest_prefab" })
            {
                if (!objectPool.TryGetValue(path, out var forest))
                {
                    forest = GameObject.Find(path);
                    if (!forest && path.Contains("/"))
                    {
                        var split = path.Split('/');
                        var tr = GameObject.Find(split[0])?.transform.Find(path[(split[0].Length + 1)..]);
                        if (tr != null)
                            forest = tr.gameObject;
                    }
                    if (forest != null)
                        objectPool.Add(path, forest);
                }

                if (forest == null)
                    continue;
                for (int i = 0; i < forest.transform.childCount; i++)
                {
                    var child = forest.transform.GetChild(i).gameObject;
                    if (!child.name.Contains(_leavesName))
                        continue;

                    child.SetActive(false);
                    leaves.Add(child);
                }
            }
        }

        public static void addleaves()
        {
            foreach (var l in leaves)
                l.SetActive(true);

            leaves.Clear();
        }

        public static void FPS(int aa) => Application.targetFrameRate = aa;

        public static void NoTagFreeze()
        {
            GorillaTagger.Instance.statusEndTime = 0f;
            GorillaTagger.Instance.currentStatus = GorillaTagger.StatusEffect.None;
            GTPlayer.Instance.disableMovement = false;
        }

        private static float tagAuraCooldown;
        public static void TagAura(float radius = 3.5f)
        {
            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null) return;
            if (!VRRig.LocalRig.mainSkin.material.name.Contains("fected")) return;
            if (Time.time < tagAuraCooldown) return;

            Vector3 localHead = GorillaTagger.Instance.headCollider.transform.position;
            float effectiveRadius = radius * (hitboxExpander ? hitboxExpanderMultiplier : 1f);
            foreach (VRRig targetRig in VRRigCache.ActiveRigs)
            {
                if (targetRig == null || targetRig.isLocal) continue;

                if (!targetRig.mainSkin.material.name.Contains("fected"))
                {
                    Vector3 targetHead = targetRig.headConstraint != null ? targetRig.headConstraint.position : targetRig.transform.position;
                    if (Vector3.Distance(localHead, targetHead) <= effectiveRadius)
                    {
                        tagAuraCooldown = Time.time + 0.35f;
                        GameMode.ReportTag(RigManager.GetPlayerFromVRRig(targetRig));
                        break;
                    }
                }
            }
        }

        private static float tagAssistCooldown;
        public static VRRig tagAssistTarget;

        public static void TagAssist(float assistRange = 8.5f, float pullSpeed = 22f)
        {
            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null) return;
            if (!VRRig.LocalRig.mainSkin.material.name.Contains("fected")) return;

            Vector3 localHead = GorillaTagger.Instance.headCollider.transform.position;
            VRRig closestTarget = null;
            float effectiveRange = assistRange * (hitboxExpander ? hitboxExpanderMultiplier : 1f);
            float closestDistance = effectiveRange;

            foreach (VRRig targetRig in VRRigCache.ActiveRigs)
            {
                if (targetRig == null || targetRig.isLocal || targetRig == VRRig.LocalRig) continue;

                if (!targetRig.mainSkin.material.name.Contains("fected"))
                {
                    Vector3 targetPos = targetRig.headConstraint != null ? targetRig.headConstraint.position : targetRig.transform.position;
                    float distance = Vector3.Distance(localHead, targetPos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = targetRig;
                    }
                }
            }

            tagAssistTarget = closestTarget;

            if (tagAssistTarget != null)
            {
                Vector3 targetHead = tagAssistTarget.headConstraint != null ? tagAssistTarget.headConstraint.position : tagAssistTarget.transform.position;
                Vector3 toTarget = (targetHead - localHead).normalized;
                GorillaTagger.Instance.rigidbody.linearVelocity = toTarget * pullSpeed;
                GorillaTagger.Instance.rightHandTransform.position = targetHead;
                if (VRRig.LocalRig != null)
                {
                    VRRig.LocalRig.rightHandTransform.position = targetHead;
                }

                if (closestDistance <= 4.5f * (hitboxExpander ? hitboxExpanderMultiplier : 1f) && Time.time > tagAssistCooldown)
                {
                    tagAssistCooldown = Time.time + 0.3f;
                    GameMode.ReportTag(RigManager.GetPlayerFromVRRig(tagAssistTarget));
                }
            }
        }

        [Setting] public static bool hitboxExpander;
        public static float hitboxExpanderMultiplier = 1.75f;

        public static void HitboxExpander()
        {
            hitboxExpander = true;
            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null) return;
            if (!VRRig.LocalRig.mainSkin.material.name.Contains("fected")) return;

            Vector3 rHand = GorillaTagger.Instance.rightHandTransform.position;
            Vector3 lHand = GorillaTagger.Instance.leftHandTransform.position;
            float reach = 0.85f * hitboxExpanderMultiplier;

            foreach (VRRig target in VRRigCache.ActiveRigs)
            {
                if (target == null || target.isLocal || target == VRRig.LocalRig) continue;
                if (target.mainSkin != null && target.mainSkin.material.name.Contains("fected")) continue;

                Vector3 targetPos = target.headConstraint != null ? target.headConstraint.position : target.transform.position;
                if (Vector3.Distance(rHand, targetPos) <= reach || Vector3.Distance(lHand, targetPos) <= reach)
                {
                    GameMode.ReportTag(RigManager.GetPlayerFromVRRig(target));
                    break;
                }
            }
        }

        public static void DisableHitboxExpander() => hitboxExpander = false;

        public static void SuperSwim(float power = 22f)
        {
            if (GTPlayer.Instance == null || GorillaTagger.Instance == null) return;
            if (GTPlayer.Instance.InWater || GTPlayer.Instance.HeadInWater)
            {
                Vector3 vel = GorillaTagger.Instance.rigidbody.linearVelocity;
                if (vel.sqrMagnitude > 0.05f)
                {
                    GorillaTagger.Instance.rigidbody.AddForce(vel.normalized * power, ForceMode.Acceleration);
                }
            }
        }

        public static void TagPull(float range = 7f, float speed = 18f)
        {
            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null) return;
            if (!VRRig.LocalRig.mainSkin.material.name.Contains("fected")) return;
            if (!InputHandler.Instance.RightTrigger.IsPressed) return;

            VRRig target = RigManager.GetClosestUntaggedVRRig(range);
            if (target != null)
            {
                Vector3 localHead = GorillaTagger.Instance.headCollider.transform.position;
                Vector3 targetHead = target.headConstraint != null ? target.headConstraint.position : target.transform.position;
                Vector3 dir = (targetHead - localHead).normalized;
                GorillaTagger.Instance.rigidbody.linearVelocity = dir * speed;
            }
        }
    }
}