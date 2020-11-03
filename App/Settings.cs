using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace KFServerPerks
{
    public class Settings
    {
        public bool AllowAll = false;
        public int ServerPort = 6000;
        public string ServerPassword = "nope";
        public List<string> Whitelist = new List<string>();

        public string MySQLHost = "127.0.0.1";
        public string MySQLUsername = "test";
        public string MySQLPasswword = "test";
        public string MySQLDatabase = "killingfloor";
        public string MySQLPerksTable = "perks";
        public int MySQLPort = 3306;

        public static Settings Load()
        {
            if (!File.Exists("config.json"))
                File.WriteAllText("config.json", JsonConvert.SerializeObject(new Settings(), Formatting.Indented));

            return JsonConvert.DeserializeObject<Settings>(File.ReadAllText("config.json"));
        }
    }
}