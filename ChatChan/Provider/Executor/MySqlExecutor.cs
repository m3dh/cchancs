﻿namespace ChatChan.Provider.Executor
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Threading.Tasks;
    using ChatChan.Common.Configuration;
    using Microsoft.Extensions.Logging;
    using MySql.Data.MySqlClient;

    public interface ISqlRecord
    {
        Task Fill(DbDataReader reader);
    }

    public static class SqlRecordHelper
    {
        public static TOut ReadColumn<TOut>(this IDataReader record, string column, Func<int, TOut> reader)
        {
            int ordinalNumber = record.GetOrdinal(column);
            return reader(ordinalNumber);
        }
    }

    public class MySqlExecutor
    {
        private readonly string connectionString;
        private readonly ILogger logger;

        public MySqlExecutor(MySqlDbSection mysqlSettings, ILoggerFactory loggerFactory)
            : this(mysqlSettings.Mode, mysqlSettings.Server, mysqlSettings.Port, mysqlSettings.Uid, mysqlSettings.Password, mysqlSettings.DbName, loggerFactory)
        {
        }

        public MySqlExecutor(string mode, string serverHost, uint port, string userId, string password, string dbName, ILoggerFactory loggerFactory)
        {
            MySqlConnectionStringBuilder connectionStringBuilder = new MySqlConnectionStringBuilder
            {
                Server = serverHost,
                Port = port,
                UserID = userId,
                Password = password,
                Database = dbName
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
                        cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }
                }

                try
                {
                    int affected = await cmd.ExecuteNonQueryAsync();
                    long lastId = affected == 1 ? cmd.LastInsertedId : -1;
                    return Tuple.Create(affected, lastId);
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
                        cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
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
    }
}
