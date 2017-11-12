using System;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.Query;

namespace MultiShardMarsError
{
    public class HealthcheckDatabaseQuery : IMultiShardDatabaseQuery<object, int>
    {
        public Func<MultiShardConnection, Task<int>> CommandFunc(object criteria)
        {
            return async databaseConnection =>
            {
                using (var command = databaseConnection.CreateCommand())
                {
                    command.CommandText = "select 1;";
                    command.CommandTimeout = 30;
                    command.CommandTimeoutPerShard = 15;

                    return await ExecuteReader(command);
                }
            };
        }

        private static async Task<int> ExecuteReader(MultiShardCommand command)
        {
            var rows = 0;
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    rows++;
                }
            }
            return rows;
        }
    }
}