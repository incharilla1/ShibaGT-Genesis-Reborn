using ExitGames.Client.Photon;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        public class ProjectileEntry
        {
            public string Name;
            public SnowballThrowable ThrowableLeft;
            public SnowballThrowable ThrowableRight;
            public SnowballThrowable Throwable => ThrowableRight ?? ThrowableLeft;
            public int ThrowableIndex => Throwable != null ? Throwable.throwableMakerIndex : -1;
        }

        public enum ThrowableHand
        {
            Left,
            Right,
            Both,
            Dynamic
        }

        [Setting] public static bool biig;
        [Setting] public static bool rainbowProjectiles;
        [Setting] public static int projectileSpeedIndex;
        private static readonly float[] projectileDelays = { 0.60f, 0.30f, 0.20f, 0.10f };
        public static readonly string[] projectileSpeedNames = { "Normal", "Fast", "Quick", "Insane" };

        private static ProjectileEntry _snowballEntry;
        private static bool _isInitializing;
        private static float projectileDelay;
        private static float orbitAngle;

        private static int _lastSentSize = -1;
        private static int _lastSentIndex = -1;

        public static float GetProjectileDelay() => projectileDelays[projectileSpeedIndex % projectileDelays.Length];

        public static void InitializeSnowball()
        {
            if (_snowballEntry != null || _isInitializing) return;
            if (CosmeticsController.instance == null || CosmeticsController.instance.v2_allCosmetics == null) return;

            _isInitializing = true;
            try
            {
                foreach (var info in CosmeticsController.instance.v2_allCosmetics)
                {
                    if (info.isThrowable && info.displayName.Contains("Snowball", StringComparison.OrdinalIgnoreCase))
                    {
                        if (CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(false, info.throwableIndex, out string rightId) &&
                            CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(true, info.throwableIndex, out string leftId))
                        {
                            var registry = VRRig.LocalRig?.cosmeticsObjectRegistry;
                            if (registry != null)
                            {
                                registry.Cosmetic(leftId);
                                registry.Cosmetic(rightId);

                                GrowingSnowballThrowable left = null, right = null;
                                foreach (var sb in SnowballMaker.leftHandInstance?.snowballs ?? Array.Empty<SnowballThrowable>())
                                {
                                    if (sb is GrowingSnowballThrowable gsb && sb.throwableMakerIndex == info.throwableIndex)
                                        left = gsb;
                                }
                                foreach (var sb in SnowballMaker.rightHandInstance?.snowballs ?? Array.Empty<SnowballThrowable>())
                                {
                                    if (sb is GrowingSnowballThrowable gsb && sb.throwableMakerIndex == info.throwableIndex)
                                        right = gsb;
                                }

                                if (left != null && right != null)
                                {
                                    left.velocityEstimator = SnowballMaker.leftHandInstance.velocityEstimator;
                                    right.velocityEstimator = SnowballMaker.rightHandInstance.velocityEstimator;

                                    _snowballEntry = new ProjectileEntry
                                    {
                                        Name = "Growing Snowball",
                                        ThrowableLeft = left,
                                        ThrowableRight = right
                                    };
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"InitializeSnowball failed: {ex}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public static void UpdateNetworkedProjectile(int index, ThrowableHand hand)
        {
            if (VRRig.LocalRig == null) return;
            if (hand == ThrowableHand.Left || hand == ThrowableHand.Both)
                VRRig.LocalRig.LeftThrowableProjectileIndex = index;
            if (hand == ThrowableHand.Right || hand == ThrowableHand.Both)
                VRRig.LocalRig.RightThrowableProjectileIndex = index;
            VRRig.LocalRig.myBodyDockPositions?.RefreshTransferrableItems();
        }

        public static void SendSnowball(Vector3 position, Vector3 velocity, Color? color = null, ThrowableHand hand = ThrowableHand.Dynamic, int forcedScale = -1)
        {
            if (Time.time < projectileDelay) return;
            projectileDelay = Time.time + GetProjectileDelay();

            try
            {
                if (_snowballEntry == null)
                {
                    InitializeSnowball();
                    if (_snowballEntry == null) return;
                }

                Color32 finalColor = color ?? (rainbowProjectiles ? Color.HSVToRGB(Mathf.Repeat(Time.time * 2f, 1f), 1f, 1f) : Color.white);
                GrowingSnowballThrowable throwable = (hand == ThrowableHand.Left ? _snowballEntry.ThrowableLeft : _snowballEntry.ThrowableRight) as GrowingSnowballThrowable;
                if (throwable == null) throwable = _snowballEntry.Throwable as GrowingSnowballThrowable;
                if (throwable == null) return;

                if (_lastSentIndex != _snowballEntry.ThrowableIndex)
                {
                    _lastSentIndex = _snowballEntry.ThrowableIndex;
                    UpdateNetworkedProjectile(_snowballEntry.ThrowableIndex, hand);
                }

                if (VRRig.LocalRig != null)
                {
                    VRRig.LocalRig.LeftThrowableProjectileColor = finalColor;
                    VRRig.LocalRig.RightThrowableProjectileColor = finalColor;
                    VRRig.LocalRig.reliableState?.SetIsDirty();
                }

                int scale = forcedScale >= 0 ? forcedScale : (biig ? 5 : 0);
                float scaleValue = (throwable.snowballSizeLevels != null && scale < throwable.snowballSizeLevels.Count) ? throwable.snowballSizeLevels[scale].snowballScale : 1f;

                Vector3 validOrigin = position;
                if (VRRig.LocalRig != null && Vector3.Distance(VRRig.LocalRig.transform.position, position) > 3.5f)
                {
                    Vector3 handPos = hand == ThrowableHand.Left
                        ? GTPlayer.Instance.LeftHand.controllerTransform.position
                        : GTPlayer.Instance.RightHand.controllerTransform.position;
                    validOrigin = handPos;
                }

                Vector3 safeVelocity = Vector3.ClampMagnitude(velocity, 48f);

                SlingshotProjectile proj = throwable.SpawnGrowingSnowball(ref safeVelocity, scaleValue);
                if (proj != null)
                {
                    int index = ProjectileTracker.AddAndIncrementLocalProjectile(proj, safeVelocity, position, scaleValue);
                    index = ((index % 50) + 50) % 50;

                    proj.Launch(position, safeVelocity, VRRig.LocalRig?.Creator ?? NetworkSystem.Instance.LocalPlayer, false, false, index, scaleValue, true, finalColor);
                    proj.OnImpact += throwable.OnProjectileImpact;

                    if (NetworkSystem.Instance.InRoom)
                    {
                        if (_lastSentSize != scale)
                        {
                            _lastSentSize = scale;
                            throwable.changeSizeEvent?.RaiseOthers(scale);
                        }

                        throwable.snowballThrowEvent?.RaiseOthers(validOrigin, safeVelocity, index);
                        RPCProt();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SendSnowball error: {e}");
            }
        }

        public static void SnowballSpam(Vector3 velocity, Vector3 origin)
        {
            bool fire = InputHandler.Instance.RightSecondary.IsPressed || (Mouse.current != null && Mouse.current.rightButton.isPressed);
            if (fire)
                SendSnowball(origin, velocity, null, ThrowableHand.Right);
        }

        public static void SnowballLauncher()
        {
            if (InputHandler.Instance == null) return;

            if (InputHandler.Instance.RightGrip.WasPressed)
            {
                Vector3 origin = GTPlayer.Instance.RightHand.controllerTransform.position;
                Vector3 velocity = GTPlayer.Instance.RightHand.controllerTransform.forward * 40f;
                SendSnowball(origin, velocity, null, ThrowableHand.Right);
            }

            if (InputHandler.Instance.LeftGrip.WasPressed)
            {
                Vector3 origin = GTPlayer.Instance.LeftHand.controllerTransform.position;
                Vector3 velocity = GTPlayer.Instance.LeftHand.controllerTransform.forward * 40f;
                SendSnowball(origin, velocity, null, ThrowableHand.Left);
            }
        }

        public static void ProjectileGun()
        {
            GunLib.StartGun(() =>
            {
                Vector3 targetPos = GunLib.GetPointerPos();
                Vector3 origin = GTPlayer.Instance.RightHand.controllerTransform.position;
                Vector3 direction = (targetPos - origin).normalized;
                SendSnowball(origin, direction * 35f, null, ThrowableHand.Right);
            }, false);
        }

        public static void SnowballAimbot()
        {
            VRRig closestRig = RigManager.GetClosestVRRig();
            if (closestRig != null)
            {
                bool trigger = InputHandler.Instance.RightTrigger.IsPressed || (Mouse.current != null && Mouse.current.leftButton.isPressed);
                if (trigger)
                {
                    Vector3 targetHead = closestRig.headConstraint != null ? closestRig.headConstraint.position : closestRig.transform.position + Vector3.up * 0.3f;
                    Vector3 origin = GTPlayer.Instance.RightHand.controllerTransform.position;
                    Vector3 velocity = (targetHead - origin).normalized * 40f;
                    SendSnowball(origin, velocity, null, ThrowableHand.Right);
                }
            }
        }

        public static void SnowballOrbit()
        {
            Vector3 center = GorillaTagger.Instance.headCollider.transform.position;
            orbitAngle += Time.deltaTime * 360f;
            float rad = orbitAngle * Mathf.Deg2Rad;
            float radius = 1.25f;

            Vector3 orbitPos = center + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(Time.time * 4f) * 0.3f, Mathf.Sin(rad) * radius);
            Vector3 tangentVel = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * 15f;

            SendSnowball(orbitPos, tangentVel, null, ThrowableHand.Dynamic);
        }

        public static void SnowballRain()
        {
            Vector3 center = GorillaTagger.Instance.headCollider != null
                ? GorillaTagger.Instance.headCollider.transform.position
                : GTPlayer.Instance.transform.position;
            Vector2 circle = UnityEngine.Random.insideUnitCircle * 1.5f;
            Vector3 spawnPos = center + new Vector3(circle.x, 3.0f, circle.y);
            Vector3 downVel = new Vector3(circle.x * 2f, -38f, circle.y * 2f);

            SendSnowball(spawnPos, downVel, null, ThrowableHand.Dynamic);
        }

        public static void FlingGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null)
                {
                    bypasstp(GunLib.LockedPlayer.transform.position, true);

                    Vector3 launchPos = GunLib.LockedPlayer.transform.position - new Vector3(0f, 0.4f, 0f);
                    Vector3 upVel = Vector3.up * 45f;
                    SendSnowball(launchPos, upVel, Color.red, ThrowableHand.Right, 5);
                }
            }, true);
        }
    }
}
