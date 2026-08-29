using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag.CosmeticSystem;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        [Setting] public static bool biig;
        [Setting] public static bool rainbowProjectiles;
        [Setting] public static int projectileSpeedIndex;
        [Setting] public static int SelectedProjectileIndex;

        public static readonly string[] ProjectileTypeNames =
        {
            "Snowball",
            "Water Balloon",
            "Lava Rock",
            "Mentos",
            "Popcorn",
            "Candy Corn",
            "Book",
            "Ice Cream",
            "Fish",
            "Present",
            "Apple"
        };

        public static readonly string[] projectileSpeedNames = { "Slow", "Fast", "Quick" };

        private static readonly float[] delayRates = { 0.7f, 0.35f, 0.20f };
        private static readonly SnowballThrowable[] leftPresets = new SnowballThrowable[11];
        private static readonly SnowballThrowable[] rightPresets = new SnowballThrowable[11];

        private static bool initialized;
        private static float nextFire;
        private static float orbitAngle;
        private static float haloAngle;
        private static bool altHand;
        private static int lastScale = -1;

        public static float FireCooldown => delayRates[projectileSpeedIndex % delayRates.Length];

        private static void InitPresets()
        {
            if (initialized || CosmeticsController.instance?.v2_allCosmetics == null || VRRig.LocalRig?.cosmeticsObjectRegistry == null) return;

            CosmeticItemRegistry reg = VRRig.LocalRig.cosmeticsObjectRegistry;

            foreach (CosmeticInfoV2 item in CosmeticsController.instance.v2_allCosmetics)
            {
                if (!item.isThrowable) continue;

                string name = item.displayName ?? item.playFabID;
                int idx = -1;
                if (name.IndexOf("Snowball", StringComparison.OrdinalIgnoreCase) >= 0) idx = 0;
                else if (name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Balloon", StringComparison.OrdinalIgnoreCase) >= 0) idx = 1;
                else if (name.IndexOf("Lava", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Rock", StringComparison.OrdinalIgnoreCase) >= 0) idx = 2;
                else if (name.IndexOf("Mentos", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Soda", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Mint", StringComparison.OrdinalIgnoreCase) >= 0) idx = 3;
                else if (name.IndexOf("Candy Corn", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Candy", StringComparison.OrdinalIgnoreCase) >= 0) idx = 5;
                else if (name.IndexOf("Popcorn", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Corn", StringComparison.OrdinalIgnoreCase) >= 0) idx = 4;
                else if (name.IndexOf("Book", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Tome", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Spell", StringComparison.OrdinalIgnoreCase) >= 0) idx = 6;
                else if (name.IndexOf("Ice Cream", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Icecream", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Sundae", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Cone", StringComparison.OrdinalIgnoreCase) >= 0) idx = 7;
                else if (name.IndexOf("Fish", StringComparison.OrdinalIgnoreCase) >= 0) idx = 8;
                else if (name.IndexOf("Present", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Gift", StringComparison.OrdinalIgnoreCase) >= 0) idx = 9;
                else if (name.IndexOf("Apple", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Fruit", StringComparison.OrdinalIgnoreCase) >= 0) idx = 10;

                if (idx < 0 || rightPresets[idx] != null) continue;

                if (CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(false, item.throwableIndex, out string rId) &&
                    CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(true, item.throwableIndex, out string lId))
                {
                    reg.Cosmetic(lId);
                    reg.Cosmetic(rId);

                    foreach (SnowballThrowable sb in SnowballMaker.leftHandInstance?.snowballs ?? Array.Empty<SnowballThrowable>())
                    {
                        if (sb != null && sb.throwableMakerIndex == item.throwableIndex)
                        {
                            leftPresets[idx] = sb;
                            if (SnowballMaker.leftHandInstance != null) sb.velocityEstimator = SnowballMaker.leftHandInstance.velocityEstimator;
                            break;
                        }
                    }

                    foreach (SnowballThrowable sb in SnowballMaker.rightHandInstance?.snowballs ?? Array.Empty<SnowballThrowable>())
                    {
                        if (sb != null && sb.throwableMakerIndex == item.throwableIndex)
                        {
                            rightPresets[idx] = sb;
                            if (SnowballMaker.rightHandInstance != null) sb.velocityEstimator = SnowballMaker.rightHandInstance.velocityEstimator;
                            break;
                        }
                    }
                }
            }

            initialized = rightPresets[0] != null;
        }

        private static SnowballThrowable GetThrowable(int index, bool isLeft)
        {
            InitPresets();
            int idx = Mathf.Clamp(index, 0, 10);
            SnowballThrowable t = isLeft ? (leftPresets[idx] ?? rightPresets[idx]) : (rightPresets[idx] ?? leftPresets[idx]);
            return t ?? (isLeft ? leftPresets[0] : rightPresets[0]);
        }

        public static void CycleProjectileType()
        {
            SelectedProjectileIndex = (SelectedProjectileIndex + 1) % ProjectileTypeNames.Length;
            SnowballThrowable cur = GetThrowable(SelectedProjectileIndex, false);
            if (cur != null) SyncThrowable(cur.throwableMakerIndex);

            ButtonInfo btn = Main.GetIndex("projtype");
            if (btn != null) btn.overlapText = $"Projectile: {ProjectileTypeNames[SelectedProjectileIndex].ToLower()}";
        }

        private static void SyncThrowable(int id)
        {
            if (VRRig.LocalRig == null) return;
            VRRig.LocalRig.LeftThrowableProjectileIndex = id;
            VRRig.LocalRig.RightThrowableProjectileIndex = id;
            VRRig.LocalRig.reliableState?.SetIsDirty();
            VRRig.LocalRig.myBodyDockPositions?.RefreshTransferrableItems();
        }

        public static void FireProjectile(Vector3 pos, Vector3 vel, bool isLeft = false, Color? tint = null, int forcedScale = -1, int typeIndex = -1)
        {
            if (Time.time < nextFire) return;
            nextFire = Time.time + FireCooldown;

            try
            {
                int idx = typeIndex >= 0 ? typeIndex : SelectedProjectileIndex;
                SnowballThrowable throwable = GetThrowable(idx, isLeft);
                if (throwable == null) return;

                SyncThrowable(throwable.throwableMakerIndex);

                Color32 col = tint ?? (rainbowProjectiles ? Color.HSVToRGB(Mathf.Repeat(Time.time * 2f, 1f), 1f, 1f) : Color.white);
                if (VRRig.LocalRig != null)
                {
                    VRRig.LocalRig.LeftThrowableProjectileColor = col;
                    VRRig.LocalRig.RightThrowableProjectileColor = col;
                    VRRig.LocalRig.reliableState?.SetIsDirty();
                }

                Vector3 origin = pos;
                if (VRRig.LocalRig != null && Vector3.Distance(VRRig.LocalRig.transform.position, pos) > 3.5f)
                {
                    origin = isLeft
                        ? GTPlayer.Instance.LeftHand.controllerTransform.position
                        : GTPlayer.Instance.RightHand.controllerTransform.position;
                }

                Vector3 speed = Vector3.ClampMagnitude(vel, 48f);
                SlingshotProjectile projectile = null;
                float scale = 1f;

                if (throwable is GrowingSnowballThrowable growing)
                {
                    int lvl = forcedScale >= 0 ? forcedScale : (biig ? 5 : 0);
                    scale = (growing.snowballSizeLevels != null && lvl < growing.snowballSizeLevels.Count)
                        ? growing.snowballSizeLevels[lvl].snowballScale
                        : 1f;

                    projectile = growing.SpawnGrowingSnowball(ref speed, scale);
                    if (projectile != null && NetworkSystem.Instance.InRoom && lastScale != lvl)
                    {
                        lastScale = lvl;
                        growing.changeSizeEvent?.RaiseOthers(lvl);
                    }
                }
                else if (throwable.projectilePrefab != null && ObjectPools.instance != null)
                {
                    GameObject spawned = ObjectPools.instance.Instantiate(throwable.projectilePrefab);
                    if (spawned != null) projectile = spawned.GetComponent<SlingshotProjectile>();
                }

                if (projectile != null)
                {
                    int index = ProjectileTracker.AddAndIncrementLocalProjectile(projectile, speed, origin, scale);
                    index = ((index % 50) + 50) % 50;

                    projectile.Launch(origin, speed, VRRig.LocalRig?.Creator ?? NetworkSystem.Instance.LocalPlayer, false, false, index, scale, true, col);
                    projectile.OnImpact += throwable.OnProjectileImpact;

                    if (NetworkSystem.Instance.InRoom)
                    {
                        if (throwable is GrowingSnowballThrowable g)
                            g.snowballThrowEvent?.RaiseOthers(origin, speed, index);
                        else
                            RoomSystem.SendLaunchProjectile(origin, speed, isLeft ? RoomSystem.ProjectileSource.LeftHand : RoomSystem.ProjectileSource.RightHand, index, true, col.r, col.g, col.b, col.a);

                        RPCProt();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ShibaGT FireProjectile error: {ex}");
            }
        }

        public static void ProjectileGun()
        {
            GunLib.StartGun(() =>
            {
                Vector3 origin = GTPlayer.Instance.RightHand.controllerTransform.position;
                Vector3 dir = (GunLib.GetPointerPos() - origin).normalized * 38f;
                FireProjectile(origin, dir, false);
            }, false);
        }

        public static void ProjectileSpam(Vector3 vel, Vector3 origin)
        {
            bool active = InputHandler.Instance.RightSecondary.IsPressed || (Mouse.current != null && Mouse.current.rightButton.isPressed);
            if (active) FireProjectile(origin, vel, false);
        }

        public static void DualProjectileSpam()
        {
            bool active = InputHandler.Instance.RightSecondary.IsPressed || InputHandler.Instance.RightTrigger.IsPressed || (Mouse.current != null && Mouse.current.rightButton.isPressed);
            if (active)
            {
                altHand = !altHand;
                Transform hand = altHand ? GTPlayer.Instance.RightHand.controllerTransform : GTPlayer.Instance.LeftHand.controllerTransform;
                FireProjectile(hand.position, hand.forward * 40f, !altHand);
            }
        }

        public static void ProjectileShotgun()
        {
            bool fired = InputHandler.Instance.RightTrigger.WasPressed || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (fired)
            {
                Transform hand = GTPlayer.Instance.RightHand.controllerTransform;
                Vector3 basePos = hand.position;
                Vector3 fwd = hand.forward;

                Vector3[] offsets =
                {
                    Vector3.zero,
                    hand.right * 0.12f,
                    -hand.right * 0.12f,
                    hand.up * 0.12f,
                    -hand.up * 0.12f,
                    (hand.right + hand.up) * 0.08f
                };

                foreach (Vector3 offset in offsets)
                    FireProjectile(basePos, (fwd + offset).normalized * 38f, false);
            }
        }

        public static void ProjectileLauncher()
        {
            if (InputHandler.Instance == null) return;

            if (InputHandler.Instance.RightGrip.WasPressed)
            {
                Transform r = GTPlayer.Instance.RightHand.controllerTransform;
                FireProjectile(r.position, r.forward * 40f, false);
            }

            if (InputHandler.Instance.LeftGrip.WasPressed)
            {
                Transform l = GTPlayer.Instance.LeftHand.controllerTransform;
                FireProjectile(l.position, l.forward * 40f, true);
            }
        }

        public static void ProjectileAimbot()
        {
            VRRig target = RigManager.GetClosestVRRig();
            if (target != null)
            {
                bool trigger = InputHandler.Instance.RightTrigger.IsPressed || (Mouse.current != null && Mouse.current.leftButton.isPressed);
                if (trigger)
                {
                    Vector3 head = target.headConstraint != null ? target.headConstraint.position : target.transform.position + Vector3.up * 0.3f;
                    Vector3 start = GTPlayer.Instance.RightHand.controllerTransform.position;
                    Vector3 dir = (head - start).normalized * 42f;
                    FireProjectile(start, dir, false);
                }
            }
        }

        public static void ProjectileOrbit()
        {
            Vector3 center = GorillaTagger.Instance.headCollider.transform.position;
            orbitAngle += Time.deltaTime * 360f;
            float rad = orbitAngle * Mathf.Deg2Rad;

            Vector3 pos = center + new Vector3(Mathf.Cos(rad) * 1.25f, Mathf.Sin(Time.time * 4f) * 0.3f, Mathf.Sin(rad) * 1.25f);
            Vector3 vel = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * 15f;
            FireProjectile(pos, vel, false);
        }

        public static void ProjectileRain()
        {
            Vector3 center = GorillaTagger.Instance.headCollider.transform.position;
            Vector2 disk = UnityEngine.Random.insideUnitCircle * 1.5f;
            Vector3 pos = center + new Vector3(disk.x, 3.0f, disk.y);
            Vector3 vel = new Vector3(disk.x * 2f, -38f, disk.y * 2f);

            FireProjectile(pos, vel, false);
        }

        public static void ProjectileHalo()
        {
            Vector3 center = GorillaTagger.Instance.headCollider.transform.position + Vector3.up * 0.35f;
            haloAngle += Time.deltaTime * 420f;
            float rad = haloAngle * Mathf.Deg2Rad;

            Vector3 pos = center + new Vector3(Mathf.Cos(rad) * 0.45f, 0f, Mathf.Sin(rad) * 0.45f);
            Vector3 vel = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * 8f;
            FireProjectile(pos, vel, false);
        }

        public static void ProjectileMortarStrike()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null)
                {
                    Vector2 scatter = UnityEngine.Random.insideUnitCircle * 0.8f;
                    Vector3 pos = GunLib.LockedPlayer.transform.position + new Vector3(scatter.x, 4.5f, scatter.y);
                    Vector3 vel = new Vector3(scatter.x, -45f, scatter.y);
                    FireProjectile(pos, vel, false, Color.red);
                }
            }, true);
        }

        public static void FlingGun()
        {
            GunLib.StartGun(() =>
            {
                if (GunLib.LockedPlayer != null)
                {
                    bypasstp(GunLib.LockedPlayer.transform.position, true);
                    Vector3 origin = GunLib.LockedPlayer.transform.position - new Vector3(0f, 0.4f, 0f);
                    FireProjectile(origin, Vector3.up * 45f, false, Color.red, 5);
                }
            }, true);
        }
    }
}
