namespace ChatChan.Provider
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using MySql.Data;
    using MySql.Data.MySqlClient;

    public class MySqlProvider
    {
        private readonly string connectionString;

        public MySqlProvider(string serverHost, int port, string userId, string password, string dbName)
        {
            MySqlConnectionStringBuilder connectionStringBuilder = new MySqlConnectionStringBuilder();
            connectionStringBuilder.Server = serverHost;
            connectionStringBuilder.Port = (uint)port;
            connectionStringBuilder.UserID = userId;
            connectionStringBuilder.Password = password;
            connectionStringBuilder.Database = dbName;

            this.connectionString = connectionStringBuilder.ToString();
        }

        public async Task<T> Execute<T>(string query)
        {
            return await Task.FromResult(default(T));
        }

        public async Task<IList<T>> QueryAll<T>(string query, Dictionary<string, object> parameters)
        {
            if(string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            using(MySqlConnection conn = new MySqlConnection(this.connectionString))
            {
                await conn.OpenAsync();
                using(MySqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = query;
                    if (parameters != null)
                    {
                        foreach(var parameter in parameters)
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }
                    }

                    List<T> result = new List<T>();
                    var reader = cmd.ExecuteReaderAsync().Result;

                    return result;
                }
            }
        }
    }
}
