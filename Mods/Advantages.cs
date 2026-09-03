using CXS;
using ExitGames.Client.Photon;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using static ShibaGTGenesisReborn.Menu.Main;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        public static bool IsRigInfected(VRRig rig)
        {
            if (rig == null) return false;
            NetPlayer np = RigManager.GetNetPlayerFromVRRig(rig);
            if (np != null && GorillaGameManager.instance is GorillaTagManager tagManager)
            {
                if (tagManager.isCurrentlyTag)
                    return tagManager.currentIt == np;
                if (tagManager.currentInfected != null)
                    return tagManager.currentInfected.Contains(np);
            }
            if (rig.setMatIndex == 1 || rig.setMatIndex == 2 || rig.setMatIndex == 11) return true;
            return rig.mainSkin?.material != null && rig.mainSkin.material.name.Contains("fected");
        }

        public static void TagGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null && !GunLib.LockedPlayer.isLocal && !IsRigInfected(GunLib.LockedPlayer))
                {
                    if (IsRigInfected(VRRig.LocalRig))
                        TagPlayer(GunLib.LockedPlayer);
                }
            }, true);
        }

        private static float lastReportTagTime;
        public static void TagPlayer(VRRig p, bool btp = false)
        {
            if (p == null || !IsRigInfected(VRRig.LocalRig) || IsRigInfected(p)) return;
            if (Time.time <= lastReportTagTime + 0.2f) return;

            RPCProt();
            lastReportTagTime = Time.time;

            Vector3 originalPos = GorillaTagger.Instance.headCollider.transform.position;
            Vector3 targetPos = p.headConstraint != null ? p.headConstraint.position : p.transform.position;

            CXS.CXS.TeleportPlayer(targetPos);

            PhotonView pv = RigManager.GetPhotonViewFromVRRig(VRRig.LocalRig);
            if (pv != null && PhotonNetwork.MasterClient != null)
                NetworkingLibrary.SendRigPosition(pv, targetPos, new int[] { PhotonNetwork.MasterClient.ActorNumber });

            Player targetPlayer = RigManager.GetPlayerFromVRRig(p);
            if (targetPlayer != null)
            {
                GameMode.ReportTag(targetPlayer);
                PhotonNetwork.SendAllOutgoingCommands();
            }

            CXS.CXS.TeleportPlayer(originalPos);
        }

        public static void TagAll()
        {
            if (!NetworkSystem.Instance.InRoom) return;

            if (!IsRigInfected(VRRig.LocalRig))
            {
                TagSelf();
                return;
            }

            VRRig target = null;
            float min = float.MaxValue;
            Vector3 localHead = GorillaTagger.Instance.headCollider.transform.position;

            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig != null && !rig.isLocal && rig != VRRig.LocalRig && !IsRigInfected(rig))
                {
                    Vector3 rigPos = rig.headConstraint != null ? rig.headConstraint.position : rig.transform.position;
                    float d = Vector3.Distance(localHead, rigPos);
                    if (d < min)
                    {
                        min = d;
                        target = rig;
                    }
                }
            }

            if (target != null)
            {
                TagPlayer(target, false);
            }
            else
            {
                ButtonInfo b = GetIndex("Tag All");
                if (b != null && b.enabled)
                {
                    b.enabled = false;
                    RecreateMenu();
                }
            }
        }

        private static Vector3 tagSelfOrigin;
        private static bool tagSelfActive;

        public static void DisableTagSelf()
        {
            if (tagSelfActive)
            {
                CXS.CXS.TeleportPlayer(tagSelfOrigin);
                tagSelfActive = false;
            }
            if (VRRig.LocalRig != null)
                VRRig.LocalRig.enabled = true;
        }

        public static void TagSelf()
        {
            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null) return;

            if (IsRigInfected(VRRig.LocalRig))
            {
                DisableTagSelf();
                ButtonInfo b = GetIndex("Tag Self");
                if (b != null && b.enabled)
                {
                    b.enabled = false;
                    RecreateMenu();
                }
                return;
            }

            if (!tagSelfActive)
            {
                tagSelfOrigin = GTPlayer.Instance.transform.position;
                tagSelfActive = true;
            }

            VRRig target = null;
            float min = float.MaxValue;
            Vector3 localBody = GorillaTagger.Instance.bodyCollider.transform.position;

            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig != null && !rig.isLocal && rig != VRRig.LocalRig && IsRigInfected(rig))
                {
                    Vector3 rigPos = rig.headConstraint != null ? rig.headConstraint.position : rig.transform.position;
                    float d = Vector3.Distance(localBody, rigPos);
                    if (d < min)
                    {
                        min = d;
                        target = rig;
                    }
                }
            }

            if (target != null)
            {
                RPCProt();
                Vector3 handPos = target.rightHandTransform != null ? target.rightHandTransform.position : (target.leftHandTransform != null ? target.leftHandTransform.position : target.transform.position);
                Vector3 bodyOffset = GorillaTagger.Instance.bodyCollider.transform.position - GTPlayer.Instance.transform.position;
                Vector3 targetPos = handPos - bodyOffset;

                CXS.CXS.TeleportPlayer(targetPos);

                PhotonView pv = RigManager.GetPhotonViewFromVRRig(VRRig.LocalRig);
                if (pv != null)
                {
                    Player targetPlayer = RigManager.GetPlayerFromVRRig(target);
                    int[] targets = targetPlayer != null && PhotonNetwork.MasterClient != null
                        ? new int[] { PhotonNetwork.MasterClient.ActorNumber, targetPlayer.ActorNumber }
                        : (PhotonNetwork.MasterClient != null ? new int[] { PhotonNetwork.MasterClient.ActorNumber } : null);

                    NetworkingLibrary.SendRigPosition(pv, targetPos, targets);
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

        public static void TagOnJoin()
        {
            PlayerPrefs.SetString("didTutorial", "nope");
            PlayerPrefs.SetString("tutorial", "nope");
            Hashtable hasht = new Hashtable();
            hasht.Add("didTutorial", true);
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

        public static void TagAura(float radius = 4f)
        {
            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null || !IsRigInfected(VRRig.LocalRig)) return;

            Vector3 localHead = GorillaTagger.Instance.headCollider.transform.position;
            float effectiveRadius = radius * (hitboxExpander ? hitboxExpanderMultiplier : 1f);
            foreach (VRRig targetRig in VRRigCache.ActiveRigs)
            {
                if (targetRig != null && !targetRig.isLocal && !IsRigInfected(targetRig))
                {
                    Vector3 targetHead = targetRig.headConstraint != null ? targetRig.headConstraint.position : targetRig.transform.position;
                    if (Vector3.Distance(localHead, targetHead) <= effectiveRadius)
                    {
                        TagPlayer(targetRig);
                    }
                }
            }
        }

        public static VRRig tagAssistTarget;

        public static void TagAssist(float assistRange = 9.5f, float pullSpeed = 30f)
        {
            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null || !IsRigInfected(VRRig.LocalRig)) return;

            Vector3 localHead = GorillaTagger.Instance.headCollider.transform.position;
            VRRig closestTarget = null;
            float effectiveRange = assistRange * (hitboxExpander ? hitboxExpanderMultiplier : 1f);
            float closestDistance = effectiveRange;

            foreach (VRRig targetRig in VRRigCache.ActiveRigs)
            {
                if (targetRig != null && !targetRig.isLocal && !IsRigInfected(targetRig))
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
                GorillaTagger.Instance.leftHandTransform.position = targetHead;
                if (VRRig.LocalRig != null)
                {
                    VRRig.LocalRig.rightHandTransform.position = targetHead;
                    VRRig.LocalRig.leftHandTransform.position = targetHead;
                }

                if (closestDistance <= 4.5f * (hitboxExpander ? hitboxExpanderMultiplier : 1f))
                {
                    TagPlayer(tagAssistTarget);
                }
            }
        }

        [Setting] public static bool hitboxExpander;
        public static float hitboxExpanderMultiplier = 7.0f;

        public static void HitboxExpander()
        {
            hitboxExpander = true;
            if (GorillaTagger.Instance != null)
            {
                GorillaTagger.Instance.maxTagDistance = 2.2f * hitboxExpanderMultiplier;
                GorillaTagger.Instance.SetTagRadiusOverrideThisFrame(0.12f * hitboxExpanderMultiplier);
            }

            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null || !IsRigInfected(VRRig.LocalRig)) return;

            Vector3 rHand = GorillaTagger.Instance.rightHandTransform.position;
            Vector3 lHand = GorillaTagger.Instance.leftHandTransform.position;
            Vector3 head = GorillaTagger.Instance.headCollider.transform.position;
            float reach = 1.25f * hitboxExpanderMultiplier;

            foreach (VRRig target in VRRigCache.ActiveRigs)
            {
                if (target != null && !target.isLocal && !IsRigInfected(target))
                {
                    Vector3 targetPos = target.headConstraint != null ? target.headConstraint.position : target.transform.position;
                    if (Vector3.Distance(rHand, targetPos) <= reach || Vector3.Distance(lHand, targetPos) <= reach || Vector3.Distance(head, targetPos) <= reach)
                    {
                        TagPlayer(target);
                    }
                }
            }
        }

        public static void DisableHitboxExpander()
        {
            hitboxExpander = false;
            if (GorillaTagger.Instance != null)
                GorillaTagger.Instance.maxTagDistance = 2.2f;
        }

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

        public static void TagPull(float range = 8f, float speed = 25f)
        {
            if (!NetworkSystem.Instance.InRoom || VRRig.LocalRig == null || !IsRigInfected(VRRig.LocalRig)) return;
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