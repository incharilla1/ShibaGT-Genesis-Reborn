using GorillaGameModes;
using GorillaTagScripts;
using ShibaGTGenesisReborn.Libs;

namespace ShibaGTGenesisReborn.Mods
{
    public partial class mods
    {
        private static void Infect(NetPlayer player)
        {
            if (GorillaGameManager.instance is GorillaTagManager manager && player != null && !manager.currentInfected.Contains(player))
                manager.AddInfectedPlayer(player);
        }

        private static void Cure(NetPlayer player)
        {
            if (GorillaGameManager.instance is GorillaTagManager manager && manager.currentInfected.Remove(player))
                manager.UpdateInfectionState();
        }

        public static void InfectGun() => GunLib.StartGun(() => Infect(GunLib.LockedPlayer.Creator), true);
        public static void CureGun() => GunLib.StartGun(() => Cure(GunLib.LockedPlayer.Creator), true);

        public static void InfectAll()
        {
            foreach (NetPlayer player in NetworkSystem.Instance.PlayerListOthers)
                Infect(player);
        }

        public static void CureAll()
        {
            foreach (NetPlayer player in NetworkSystem.Instance.AllNetPlayers)
                Cure(player);
        }

        private static void UnTag(NetPlayer player)
        {
            if (player == null) return;
            GorillaGameManager manager = GorillaGameManager.instance;

            if (manager is GorillaGuardianManager guardian)
            {
                if (NetworkSystem.Instance.IsMasterClient) guardian.EjectGuardian(player);
                else if (player == NetworkSystem.Instance.LocalPlayer) guardian.RequestEjectGuardian(player);
                return;
            }

            if (manager is GorillaHuntManager hunt)
            {
                hunt.currentHunted.Remove(player);
                return;
            }

            if (manager is GorillaPaintbrawlManager paint)
            {
                paint.playerLives[player.GetPlayerRef().ActorNumber] = 3;
                return;
            }

            if (manager is GorillaTagManager tag)
            {
                if (manager is GorillaFreezeTagManager freeze) freeze.currentFrozen.Remove(player);
                tag.currentInfected.Remove(player);
                tag.UpdateInfectionState();
            }
        }

        public static void UnTagSelf() => UnTag(NetworkSystem.Instance.LocalPlayer);

        public static void UnTagAll()
        {
            foreach (NetPlayer player in NetworkSystem.Instance.AllNetPlayers)
                UnTag(player);
        }

        public static void GiveGuardianGun()
        {
            if (!(GorillaGameManager.instance is GorillaGuardianManager) || !NetworkSystem.Instance.IsMasterClient) return;

            GunLib.StartGun(() =>
            {
                foreach (GorillaGuardianZoneManager zone in GorillaGuardianZoneManager.zoneManagers)
                {
                    if (zone != null && zone.IsZoneValid() && zone.CurrentGuardian != GunLib.LockedPlayer.Creator)
                        zone.SetGuardian(GunLib.LockedPlayer.Creator);
                }
            }, true);
        }
    }
}
