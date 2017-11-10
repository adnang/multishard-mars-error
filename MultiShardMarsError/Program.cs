using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.SqlDatabase.ElasticScale.Query;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace MultiShardMarsError
{
    public class Program
    {
        private const string ShardMapName = "SHARD_MAP_NAME";

        public const string ShardMgrConnectionString = "CONNECTION_STRING";
    
        public static void Main(string[] args)
        {
            var shardMap = GetShardMap();

            var threads = Enumerable.Range(0, 150).Select(
                i => new Thread(
                    () =>
                    {
                        while (true)
                        {
                            try
                            {
                                ExecuteWithRetries(shardMap);
                            }
                            catch (Exception e)
                            {

                                if (e.ToString().Contains("MultipleActiveResultSets"))
                                {
                                    Console.WriteLine(">>>>>>>>" );
                                    Console.WriteLine(e.ToString());
                                    Console.WriteLine("<<<<<<<<");
                                    throw;
                                }

                                Console.WriteLine(e.ToString());                               
                            }
                        }
                    })).ToList();

            threads.ForEach(t => t.Start());
        }

        private static RangeShardMap<int> GetShardMap()
        {
            var shardMapManager = ShardMapManagerFactory.GetSqlShardMapManager(ShardMgrConnectionString, ShardMapManagerLoadPolicy.Lazy);
            var shardMap = shardMapManager.GetRangeShardMap<int>(ShardMapName);
            return shardMap;
        }

        private static void ExecuteWithRetries(RangeShardMap<int> shardMap)
        {
            var retryPolicy = CreateIncrementalPolicy();
            retryPolicy.ExecuteAsync(() => Execute(shardMap)).GetAwaiter().GetResult();
        }

        public static async Task<int> Execute(ShardMap shardMap)
        {
            using ( var databaseConnection = new MultiShardConnection(shardMap.GetShards(),GetShardConnectionString(ShardMgrConnectionString)))
            {
                using (var command = databaseConnection.CreateCommand())
                {
                    command.CommandText = "select 1;";
                    command.CommandTimeout = 30;
                    command.CommandTimeoutPerShard = 15;

                    return await ExecuteReader(command);
                }
            }
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

        public static RetryPolicy CreateIncrementalPolicy()
        {
            var retryCount = 3;
            var initialInterval = TimeSpan.FromMilliseconds(10);
            var increment = TimeSpan.FromMilliseconds(200);

            var incrementalRetryStrategy = new Incremental(null, retryCount, initialInterval, increment, true);

            return new RetryPolicy(new SqlDatabaseTransientErrorDetectionStrategy(), incrementalRetryStrategy);
        }

        private static string GetShardConnectionString(string shardManagerConnectionString)
        {
            var shardManagerConnectionStringBuilder = new SqlConnectionStringBuilder(shardManagerConnectionString)
            {
                DataSource = string.Empty,
                InitialCatalog = string.Empty
            };

            var shardConnectionString = shardManagerConnectionStringBuilder.ToString();

            return shardConnectionString;
        }
    }
}
