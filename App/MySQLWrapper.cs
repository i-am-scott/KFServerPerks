using KFServerPerks.util;
using MySqlConnector;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;

namespace KFServerPerks
{
    public class Mysql
    {
        public readonly string host;
        public readonly string username;
        public readonly string database;
        public readonly int port;
        private string password;
        private string connectionString;

        public Mysql(string host, string username, string password, string database, int port = 3306)
        {
            this.host = host;
            this.database = database;
            this.username = username;
            this.password = password;
            this.port = port;

            connectionString = string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4}", this.host, this.database, this.username, this.password, this.port);
        }

        private void Connection_StateChange(object sender, StateChangeEventArgs e)
        {
            Logging.Log($"[MySQL] State changed {e.CurrentState}.");
        }

        public int Query(string query, OrderedDictionary args = null)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                ;
                using (var cmd = new MySqlCommand(query, conn))
                {
                    if (args != null)
                    {
                        IDictionaryEnumerator reader = args.GetEnumerator();
                        while (reader.MoveNext())
                            cmd.Parameters.AddWithValue(reader.Key.ToString(), reader.Value);
                    }

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public DataTable Query(string query, params object[] values)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new MySqlCommand(query, conn))
                {
                    foreach (object val in values)
                        cmd.Parameters.AddWithValue("?", val);

                    DataTable results = new DataTable();
                    results.Load(cmd.ExecuteReader());
                    return results;
                }
            };
        }
    }

}
