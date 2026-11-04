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

        public static void GiveGuardianGun()
        {
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
