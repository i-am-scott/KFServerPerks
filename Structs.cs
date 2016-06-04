using System;
using System.Linq;
using KFServerPerks.util;

namespace KFServerPerks
{

    [AttributeUsage(System.AttributeTargets.Method)]
    public class ENetCommandType : Attribute
    {
        public string cmdType;
        public ENetID cmdInt;

        public ENetCommandType(string name, ENetID cmd)
        {
            cmdType = name;
            cmdInt = cmd;
        }
    }

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

    public class User
    {

        public enum ID_TYPE {
            STEAMID_32,
            STEAMID_64
        }


        public int  _databaseId;
        public long   steamid64;
        public string steamid32;

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
        public string p_customPerks;

        public User(string steamid, ID_TYPE type )
        {
            
            if ( type == ID_TYPE.STEAMID_32 )
            {
                steamid64 = SteamID32To64(steamid);
                steamid32 = steamid;
            }
            else
            {
                steamid64 = long.Parse(steamid);
                steamid32 = SteamID64To32(steamid);
            }
        }

        public void SetStats( string[] stats )
        {
            var first   = stats[0].Split(new char[] { ':' });
            p_veterancy = first[0];

            int.TryParse(stats[1],  out p_perkIndex );
            int.TryParse(stats[2],  out p_damageHealed);
            int.TryParse(stats[3],  out p_weldingPoints);
            int.TryParse(stats[4],  out p_shotgunDamage);
            int.TryParse(stats[5],  out p_headshotKills);
            int.TryParse(stats[6],  out p_stalkerKills);
            int.TryParse(stats[7],  out p_bullpupDamage);
            int.TryParse(stats[8],  out p_meleeDamage);
            int.TryParse(stats[9],  out p_flamethrowerDamage);
            int.TryParse(stats[10], out p_selfHeals);
            int.TryParse(stats[11], out p_soleSurvivorWaves);
            int.TryParse(stats[12], out p_cashDonated);
            int.TryParse(stats[13], out p_feedingKills);
            int.TryParse(stats[14], out p_burningCrossbowKills);
            int.TryParse(stats[15], out p_gibbedFleshpounds);
            int.TryParse(stats[16], out p_stalkersKilledWithExplosives);
            int.TryParse(stats[17], out p_gibbedEnemies);
            int.TryParse(stats[18], out p_bloatKills);
            int.TryParse(stats[19], out p_kills);
            int.TryParse(stats[20], out p_explosivesDamage);
            int.TryParse(stats[21], out p_totalZedTime);
            int.TryParse(stats[22], out p_totalPlayTime);
            int.TryParse(stats[23], out p_wins);
            int.TryParse(stats[24], out p_lostCount);

            p_character   = stats[25];
            p_customPerks = string.Join( ",", ( new ArraySegment<string>( stats, 26, stats.Length - 26 ) ).ToArray()  );
        }

        private string SteamID64To32( string steamid64 )
        {
            long sid;
            string steamId = "";

            if (long.TryParse(steamid64, out sid))
            {
                long accountid = sid - (sid >> 32 << 32);
                int lastBit = accountid % 2 == 0 ? 0 : 1;
                steamId = "STEAM_0:" + lastBit + ":" + (accountid >> 1).ToString();
            }

            return steamId;
        }

        private long SteamID32To64( string steamid32 )
        {
            steamid32      = steamid32.Replace("STEAM_", "");
            string[] split = steamid32.Split(':');
            string   id    = "765611979" + ((Convert.ToInt64(split[2]) * 2) + 60265728 + Convert.ToInt64(split[1])).ToString();

            return long.Parse(id);
        }

        public void SaveToDatabase()
        {

            Logging.Log($"Saving user {steamid32} ({steamid64.ToString()}) to the database.");

            string query = $@"REPLACE INTO perks( p_veterancy, p_perkIndex, p_damageHealed, p_weldingPoints, p_shotgunDamage, p_headshotKills, p_stalkerKills, p_bullpupDamage,
                                                  p_meleeDamage, p_flamethrowerDamage, p_selfHeals, p_soleSurvivorWaves, p_cashDonated, p_feedingKills, p_burningCrossbowKills,
                                                  p_gibbedFleshpounds, p_stalkersKilledWithExplosives, p_gibbedEnemies, p_bloatKills, p_sirenKills, p_kills, p_explosivesDamage,
                                                  p_totalZedTime, p_totalPlayTime, p_wins, p_lostCount,  p_character,  p_customPerks, steamid32, steamid64 ) 

                                                 VALUES( :p_veterancy, :p_perkIndex, :p_damageHealed, :p_weldingPoints, :p_shotgunDamage, :p_headshotKills, :p_stalkerKills, 
                                                         :p_bullpupDamage, :p_meleeDamage, :p_flamethrowerDamage, :p_selfHeals, :p_soleSurvivorWaves, :p_cashDonated, :p_feedingKills, 
                                                         :p_burningCrossbowKills, :p_gibbedFleshpounds, :p_stalkersKilledWithExplosives, :p_gibbedEnemies, :p_bloatKills, :p_sirenKills, 
                                                         :p_kills, :p_explosivesDamage, :p_totalZedTime, :p_totalPlayTime, :p_wins, :p_lostCount, :p_character, :p_customPerks, :steamid32, 
                                                         :steamid64 )";

            Logging.Log(query);

        }

    }

}
