using System;
using System.Runtime.InteropServices;

namespace KFServerPerks
{

    public enum ENetID
    {
        ID_None = 0,
        ID_Open,
        ID_RequestPassword,
        ID_HeresPassword,
        ID_ConnectionClosed,
        ID_PasswordCorrect,
        ID_NewPlayer,
        ID_BadNewPlayer,
        ID_SendPlayerData,
        ID_UpdatePlayer,
        ID_KeepAlive
    }

    public class Stats
    {
        public string p_veterancy;
        public int p_perkIndex;
        public int p_damageHealed;
        public int p_weldingPoints;
        public int p_shotgunDamage;
        public int p_headshotKills;
        public int p_stalkerKills;
        public int p_bullpupDamage;
        public int p_meleeDamage;
        public int p_flamethrowerDamage;
        public int p_selfHeals;
        public int p_soleSurvivorWaves;
        public int p_cashDonated;
        public int p_feedingKills;
        public int p_burningCrossbowKills;
        public int p_gibbedFleshpounds;
        public int p_stalkersKilledWithExplosives;
        public int p_gibbedEnemies;
        public int p_bloatKills;
        public int p_sirenKills;
        public int p_kills;
        public int p_explosivesDamage;
        public int p_totalZedTime;
        public int p_totalPlayTime;
        public int p_wins;
        public int p_lostCount;

        public string p_character;
        public string[] p_customPerks;

        private void Populate( string playerId, string[] stats )
        {

        }

        public void SaveToDatabase()
        {

        }

        public Stats(string playerId, string[] stats)
        {
            Populate(playerId, stats);
        }

        public Stats( int playerId, string[] stats )
        {
            Populate(playerId.ToString(), stats);
        }

    }

}
