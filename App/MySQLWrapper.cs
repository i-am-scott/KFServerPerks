using KFServerPerks.util;
using MySqlConnector;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Threading.Tasks;

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

            connectionString = string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};max pool size=50;", this.host, this.database, this.username, this.password, this.port);
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

                try
                {
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
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    conn.Close();
                    conn.Dispose();
                }

                return 0;
            }
        }

        public DataTable Query(string query, params object[] values)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
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
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    conn.Close();
                    conn.Dispose();
                }
            };

            return null;
        }

        public async Task QueryAsync(string query, OrderedDictionary args = null)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    await conn.OpenAsync();

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (args != null)
                        {
                            IDictionaryEnumerator reader = args.GetEnumerator();
                            while (reader.MoveNext())
                                cmd.Parameters.AddWithValue(reader.Key.ToString(), reader.Value);
                        }

                        await cmd.ExecuteNonQueryAsync();
                        await cmd.DisposeAsync();
                    }
                
                }
                catch(Exception e)
                {
                   Console.WriteLine(e);
                }
                finally
                {
                    await conn.CloseAsync();
                    await conn.DisposeAsync();
                }
            }
        }

        public void QueryTransactions(string[] queries, OrderedDictionary[] argsArray)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new MySqlCommand())
                {
                    MySqlTransaction transaction = conn.BeginTransaction();
                    cmd.Connection = conn;
                    cmd.Transaction = transaction;
                    cmd.CommandTimeout = 5;

                    for (int i = 0; i < queries.Length; i++)
                    {
                        string query = queries[i];
                        OrderedDictionary args = argsArray[i];

                        cmd.CommandText = query;

                        IDictionaryEnumerator reader = args.GetEnumerator();
                        while (reader.MoveNext())
                            cmd.Parameters.AddWithValue(reader.Key.ToString(), reader.Value);

                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                    }

                    transaction.Commit();
                    cmd.Dispose();
                }

                conn.Close();
                conn.Dispose();
            }
        }
    }

}