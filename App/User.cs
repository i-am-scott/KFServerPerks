using KFServerPerks.util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KFServerPerks
{

    public class User : IDisposable
    {

        public enum ID_TYPE
        {
            STEAMID_32,
            STEAMID_64
        }

        private Mysql db;
        private bool disposedValue = false;

        public readonly int  _databaseId;
        public readonly long   steamid64;
        public readonly string steamid32;

        public Dictionary<string, object> _perkData;
        public Dictionary<string, object> PerkData
        {
            get { return _perkData; }
            set { }
        }

        public User(string steamid, ID_TYPE type, bool suppressMySQL = false )
        {
            if (type == ID_TYPE.STEAMID_32)
            {
                steamid64 = SteamID32To64(steamid);
                steamid32 = steamid;
            }
            else
            {
                steamid64 = long.Parse(steamid);
                steamid32 = SteamID64To32(steamid);
            }

            if ( !suppressMySQL ) 
            {
                db = new Mysql( Settings.MySQLHost, Settings.MySQLUsername, Settings.MySQLPasswword, Settings.MySQLDatabase );
                db.Connect();
            }
        }

        public void SetStats(string[] stats)
        {
            _perkData = new Dictionary<string, object>() {
                { "p_veterancy",                       stats[0] },
                { "p_perkIndex",                       int.Parse(stats[1])  },
                { "p_damageHealed",                    int.Parse(stats[2])  },
                { "p_weldingPoints",                   int.Parse(stats[3])  },
                { "p_shotgunDamage",                   int.Parse(stats[4])  },
                { "p_headshotKills",                   int.Parse(stats[5])  },
                { "p_stalkerKills",                    int.Parse(stats[6])  },
                { "p_bullpupDamage",                   int.Parse(stats[7])  },
                { "p_meleeDamage",                     int.Parse(stats[8])  },
                { "p_flamethrowerDamage",              int.Parse(stats[9])  },
                { "p_selfHeals",                       int.Parse(stats[10]) },
                { "p_soleSurvivorWaves",               int.Parse(stats[11]) },
                { "p_cashDonated",                     int.Parse(stats[12]) },
                { "p_feedingKills",                    int.Parse(stats[13]) },
                { "p_burningCrossbowKills",            int.Parse(stats[14]) },
                { "p_gibbedFleshpounds",               int.Parse(stats[15]) },
                { "p_stalkersKilledWithExplosives",    int.Parse(stats[16]) },
                { "p_gibbedEnemies",                   int.Parse(stats[17]) },
                { "p_bloatKills",                      int.Parse(stats[18]) },
                { "p_sirenKills",                      int.Parse(stats[19]) },
                { "p_kills",                           int.Parse(stats[20]) },
                { "p_explosivesDamage",                int.Parse(stats[21]) },
                { "p_totalZedTime",                    int.Parse(stats[22]) },
                { "p_totalPlayTime",                   int.Parse(stats[23]) },
                { "p_wins",                            int.Parse(stats[24]) },
                { "p_lostCount",                       int.Parse(stats[25]) },
                { "p_character",                       stats[26] },
                { "p_customPerks",                     string.Join( "," , stats.TakeWhile( ( stat, index ) => index > 26 ).ToArray()) },
                { "steamid32",                         steamid32 },
                { "steamid64",                         steamid64 }
            };
        }

        public string GetFormmatedStats()
        {
            return "";
        }

        private string SteamID64To32(string steamid64)
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

        private long SteamID32To64(string steamid32)
        {
            steamid32 = steamid32.Replace("STEAM_", "");
            string[] split = steamid32.Split(':');
            string id = "765611979" + ((Convert.ToInt64(split[2]) * 2) + 60265728 + Convert.ToInt64(split[1])).ToString();

            return long.Parse(id);
        }

        public void SaveToDatabase()
        {
            Logging.Log($"Saving user {steamid32} ({steamid64.ToString()}) to the database.");

            string[] keys    = _perkData.Keys.ToArray();
            string fields    = string.Join( ", ",  keys );
            string values    = string.Join( ", ",  keys.Select(key => ("@" + key) ));
            string query     = $@"REPLACE INTO perks({ fields }) VALUES({values});";
  
            try
            {
                db.Query( query, _perkData );
            }
            catch ( Exception E )
            {
                Logging.Log(E.Message);
            }
            finally
            {
                db.Disconnect();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    db.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }

}
