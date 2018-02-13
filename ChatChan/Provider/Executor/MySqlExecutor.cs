namespace ChatChan.Provider.Executor
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;
    using ChatChan.Common;
    using ChatChan.Common.Configuration;

    using Microsoft.Extensions.Logging;

    using MySql.Data.MySqlClient;

    public interface ISqlRecord
    {
        Task Fill(DbDataReader reader);
    }

    public static class SqlRecordHelper
    {
        public static DateTimeOffset ReadDateColumn(this IDataReader record, string column)
        {
            DateTime datetime = record.ReadColumn(column, record.GetDateTime);
            return DateTime.SpecifyKind(datetime, DateTimeKind.Utc);
        }

        public static TOut ReadColumn<TOut>(this IDataReader record, string column, Func<int, TOut> reader)
        {
            try
            {
                int ordinalNumber = record.GetOrdinal(column);
                return record.IsDBNull(ordinalNumber) ? default : reader(ordinalNumber);
            }
            catch (IndexOutOfRangeException)
            {
                return default;
            }
        }
    }

    public class MySqlExecutor : IDisposable
    {
        private static readonly string[] SupportedSqlModes = new[] { "MySQL-AutoCommit" };
        private readonly string connectionString;
        private readonly ILogger logger;

        public MySqlExecutor(MySqlDbSection mysqlSettings, Dictionary<string, string> strings, ILoggerFactory loggerFactory)
            : this(strings, mysqlSettings.Mode, mysqlSettings.Server, mysqlSettings.Port, mysqlSettings.Uid, mysqlSettings.Password, mysqlSettings.DbName, loggerFactory)
        {
        }

        public MySqlExecutor(Dictionary<string, string> strings, string mode, string serverHost, uint port, string userId, string password, string dbName, ILoggerFactory loggerFactory)
        {
            if (SupportedSqlModes.All(m => !string.Equals(m, mode, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Unexpected SQL mode {mode}", nameof(mode));
            }

            if (serverHost.StartsWith("_"))
            {
                serverHost = strings[serverHost];
            }

            MySqlConnectionStringBuilder connectionStringBuilder = new MySqlConnectionStringBuilder
            {
                Server = serverHost,
                Port = port,
                UserID = userId,
                Password = password,
                Database = dbName,
                CharacterSet = "utf8mb4",
            };

            this.logger = loggerFactory.CreateLogger<MySqlExecutor>();
            this.connectionString = connectionStringBuilder.ToString();
        }

        private async Task<MySqlConnection> GetNewConnection()
        {
            MySqlConnection conn = new MySqlConnection(this.connectionString);
            try
            {
                await conn.OpenAsync();
                return conn;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning("MySQL connection unable to start : {0}", ex.Message);
                await conn.CloseAsync();
                throw;
            }
        }

        // Returns affected, lastId tuple, since we already has tuple comprehension in C#.
        public async Task<Tuple<int, long>> Execute(string query, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            using (MySqlConnection conn = await this.GetNewConnection())
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        if (parameter.Value is DateTimeOffset offset)
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, offset.DateTime);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }
                    }
                }

                try
                {
                    int affected = await cmd.ExecuteNonQueryAsync();
                    long lastId = affected == 1 ? cmd.LastInsertedId : -1;
                    return Tuple.Create(affected, lastId);
                }
                catch (MySqlException ex)
                {
                    TryHandleMySqlException(ex);
                    throw;
                }
                catch (Exception ex)
                {
                    this.logger.LogInformation("MySQL command failed : Q {0}, Msg {1}", query, ex.Message);
                    throw;
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
        }

        public async Task<T> QueryScalar<T>(string query, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            using (MySqlConnection conn = await this.GetNewConnection())
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        if (parameter.Value is DateTimeOffset offset)
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, offset.DateTime);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }
                    }
                }

                try
                {
                    object result = await cmd.ExecuteScalarAsync();
                    Type targetType = typeof(T);
                    Type sourceType = result.GetType();
                    if (targetType == sourceType || targetType.IsAssignableFrom(sourceType))
                    {
                        return (T)result;
                    }
                    else
                    {
                        return (T)Convert.ChangeType(result, targetType);
                    }
                }
                catch (MySqlException ex)
                {
                    TryHandleMySqlException(ex);
                    throw;
                }
                catch (Exception ex)
                {
                    this.logger.LogInformation("MySQL command failed : Q {0}, Msg {1}", query, ex.Message);
                    throw;
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
        }

        public async Task<IList<T>> QueryAll<T>(string query, Dictionary<string, object> parameters = null)
            where T : ISqlRecord, new()
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            using (MySqlConnection conn = await this.GetNewConnection())
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        if (parameter.Value is DateTimeOffset offset)
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, offset.DateTime);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }
                    }
                }

                try
                {
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        List<T> result = new List<T>();
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                T record = new T();
                                await record.Fill(reader);
                                result.Add(record);
                            }
                        }

                        return result;
                        // We do assume that all data in the SQL db could be dumped, so just close the reader here...
                    }
                }
                catch (MySqlException ex)
                {
                    TryHandleMySqlException(ex);
                    throw;
                }
                catch (Exception ex)
                {
                    this.logger.LogInformation("MySQL command failed : Q {0}, Msg {1}", query, ex.Message);
                    throw;
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
        }

        // Some user errors could only be caught here, and got converted to user caused exception types.
        private static void TryHandleMySqlException(MySqlException ex)
        {
            switch (ex.Number)
            {
                case 1062:
                    throw new Conflict(Conflict.Code.Duplication, "The data modification violates some unique key constraints");

                default:
                    return;
            }
        }

        public void Dispose()
        {
            MySqlConnection.ClearAllPools();
        }
    }
}