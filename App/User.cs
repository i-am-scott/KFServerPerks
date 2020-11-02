using KFServerPerks.util;
using System;
using System.Collections.Specialized;
using System.Data;
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

        private readonly Mysql db;
        private bool disposedValue = false;

        public string Id { get => Steamid64; set { } }
        public readonly string Steamid64;
        public readonly string Steamid32;

        public string KfStringFormat { get; protected set; }
        public OrderedDictionary PerkData { get; protected set; }

        public User(string steamid, ID_TYPE type)
        {
            if (type == ID_TYPE.STEAMID_32)
            {
                Steamid64 = SteamID32To64(steamid);
                Steamid32 = steamid;
            }
            else
            {
                Steamid64 = steamid;
                Steamid32 = SteamID64To32(steamid);
            }

            string[] id32split = Steamid32.Split(new char[] { ':' });
            Id = int.Parse(id32split[1]) + 1 + id32split[2];

            db = new Mysql(Program.settings.MySQLHost, Program.settings.MySQLUsername, Program.settings.MySQLPasswword, Program.settings.MySQLDatabase);
            db.Connect();
        }

        private string SteamID64To32(string steamid64)
        {
            string steamId = "";

            if (long.TryParse(steamid64, out long sid))
            {
                long accountid = sid - (sid >> 32 << 32);
                int lastBit = accountid % 2 == 0 ? 0 : 1;
                steamId = "STEAM_0:" + lastBit + ":" + (accountid >> 1).ToString();
            }

            return steamId;
        }

        private string SteamID32To64(string steamid32)
        {
            steamid32 = steamid32.Replace("STEAM_", "");
            string[] split = steamid32.Split(':');
            string id = "765611979" + ((Convert.ToInt64(split[2]) * 2) + 60265728 + Convert.ToInt64(split[1])).ToString();

            return id;
        }

        private void PopulateStats(string vetInfo, string[] stats)
        {
            string[] vetSplit = vetInfo.Split(':');
            string veterancy = vetSplit[0];

            int perk = 255;
            if (vetSplit.Length == 2)
                int.TryParse(vetSplit[1], out perk);

            PerkData = new OrderedDictionary()
            {
                { "p_veterancy",                       veterancy },
                { "p_perkIndex",                       perk  },
                { "p_damageHealed",                    int.Parse(stats[0])  },
                { "p_weldingPoints",                   int.Parse(stats[1])  },
                { "p_shotgunDamage",                   int.Parse(stats[2])  },
                { "p_headshotKills",                   int.Parse(stats[3])  },
                { "p_stalkerKills",                    int.Parse(stats[4])  },
                { "p_bullpupDamage",                   int.Parse(stats[5])  },
                { "p_meleeDamage",                     int.Parse(stats[6])  },
                { "p_flamethrowerDamage",              int.Parse(stats[7])  },
                { "p_selfHeals",                       int.Parse(stats[8])  },
                { "p_soleSurvivorWaves",               int.Parse(stats[9]) },
                { "p_cashDonated",                     int.Parse(stats[10]) },
                { "p_feedingKills",                    int.Parse(stats[11]) },
                { "p_burningCrossbowKills",            int.Parse(stats[12]) },
                { "p_gibbedFleshpounds",               int.Parse(stats[13]) },
                { "p_stalkersKilledWithExplosives",    int.Parse(stats[14]) },
                { "p_gibbedEnemies",                   int.Parse(stats[15]) },
                { "p_bloatKills",                      int.Parse(stats[16]) },
                { "p_sirenKills",                      int.Parse(stats[17]) },
                { "p_kills",                           int.Parse(stats[18]) },
                { "p_explosivesDamage",                int.Parse(stats[19]) },
                { "p_totalZedTime",                    int.Parse(stats[20]) },
                { "p_totalPlayTime",                   int.Parse(stats[21]) },
                { "p_wins",                            int.Parse(stats[22]) },
                { "p_lostCount",                       int.Parse(stats[23]) },
                { "p_character",                       stats[24]},
                { "p_customPerks",                     string.Join( "," , stats.TakeWhile( ( stat, index ) => index > 24 ).ToArray()) },
                { "steamid32",                         Steamid32 },
                { "steamid64",                         Steamid64 }
            };

            KfStringFormat = veterancy + ":" + perk + "," + string.Join(",", stats).TrimEnd(' ').Replace(",,", ", '',");
        }

        public void SetStats(string[] stats)
        {
            PopulateStats(stats[0], stats.Skip(1).ToArray());
        }

        public void SaveToDatabase()
        {
            Logging.Log($"[PLAYER] Saving user {Steamid32} ({Steamid64}) to the database.");

            string[] keys = new string[PerkData.Keys.Count];
            PerkData.Keys.CopyTo(keys, 0);

            string fields = string.Join(", ", keys);
            string values = string.Join(", ", keys.Select(key => ("@" + key)));
            string query = $@"REPLACE INTO perks({ fields }) VALUES({values});";

            try
            {
                db.Query(query, PerkData);
            }
            catch (Exception E)
            {
                Logging.Log(E.Message);
            }
        }

        public bool LoadFromDatabase()
        {
            Logging.Log($"[PLAYER] Loading User {Steamid32} ({Steamid64}) from the database.");

            try
            {
                DataTable data = db.Query($"SELECT * FROM {Program.settings.MySQLPerksTable} WHERE steamid64 = ? LIMIT 1", Steamid64);
                if (data == null || data.Rows.Count == 0) return false;

                object[] row = data.Rows[0].ItemArray;
                string[] items = row.ToList().Skip(2).Select(val => val.ToString()).Take(row.Count() - 5).ToArray();

                PopulateStats(row[0] + ":" + row[1], items);
                return true;
            }
            catch (Exception E)
            {
                Logging.Log(E.Message);
            }

            return false;
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
