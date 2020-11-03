using KFServerPerks.util;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Linq;

namespace KFServerPerks
{
    public class User
    {
        public enum ID_TYPE
        {
            STEAMID_32,
            STEAMID_64,
            INTERNAL
        }

        private readonly Mysql db;

        public string InternalId;
        public readonly string Steamid64;
        public readonly string Steamid32;

        public string KfStringFormat { get; protected set; }
        public OrderedDictionary PerkData { get; protected set; }

        public User(string id, ID_TYPE type)
        {
            if (type == ID_TYPE.STEAMID_32)
            {
                Steamid64 = SteamID32To64(id);
                Steamid32 = id;
            }
            else if (type == ID_TYPE.STEAMID_64)
            {
                Steamid64 = id;
                Steamid32 = SteamID64To32(id);
            }

            if (type == ID_TYPE.INTERNAL)
            {
                InternalId = id;
                Steamid32 = InternalIDToSteamID32(id);
                Steamid64 = SteamID32To64(Steamid32);
            }
            else
            {
                InternalId = SteamID32ToInternalID(Steamid32);
            }

            db = new Mysql(Program.settings.MySQLHost, Program.settings.MySQLUsername, Program.settings.MySQLPasswword, Program.settings.MySQLDatabase);
        }

        private string SteamID32ToInternalID(string id)
        {
            id = Steamid32.Replace("STEAM_0", "").Replace(":", "");

            int realmId = Convert.ToInt32(id.Substring(0, 1));
            id = ++realmId + id.Remove(0, 1);

            return id;
        }

        private string InternalIDToSteamID32(string id)
        {
            int realmId = Convert.ToInt32(id.Substring(0, 1));
            string playerid = id.Substring(1);

            return $"STEAM_0:{--realmId}:{playerid}";
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
                Logging.Log(E);
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
                string[] items = row.ToList().Skip(2).Select(val => val.ToString()).Take(row.Count() - 7).ToArray();

                PopulateStats(row[0] + ":" + row[1], items);
                return true;
            }
            catch (Exception E)
            {
                Logging.Log(E.Message);
            }

            return false;
        }
    }

}
