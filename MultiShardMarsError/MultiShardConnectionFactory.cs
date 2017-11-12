using System.Configuration;
using System.Data.SqlClient;
using Microsoft.Azure.SqlDatabase.ElasticScale.Query;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

namespace MultiShardMarsError
{
    public class MultiShardConnectionFactory : IMultiShardConnectionFactory
    {
        private const string ShardMapName = "SHARD_MAP_NAME";
        private const string ShardMgrConnectionString = "CONNECTION_STRING";
        private static readonly RangeShardMap<int> RangeShardMap = GetShardMap();
        private readonly string _shardManagerConnectionString;

        public MultiShardConnectionFactory()
        {
            _shardManagerConnectionString = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString 
                                            ?? ShardMgrConnectionString;
        }

        public MultiShardConnection CreateMultiShardConnection()
        {
            return new MultiShardConnection(RangeShardMap.GetShards(), GetShardConnectionString(_shardManagerConnectionString));
        }
    
        private static RangeShardMap<int> GetShardMap() //singleton
        {
            var shardMapManager = ShardMapManagerFactory.GetSqlShardMapManager(ShardMgrConnectionString, ShardMapManagerLoadPolicy.Lazy);
            var shardMap = shardMapManager.GetRangeShardMap<int>(ShardMapName);
            return shardMap;
        }

        private static string GetShardConnectionString(string shardManagerConnectionString)
        {
            var shardManagerConnectionStringBuilder = new SqlConnectionStringBuilder(shardManagerConnectionString)
            {
                DataSource = string.Empty,
                InitialCatalog = string.Empty
            };

            return shardManagerConnectionStringBuilder.ToString();
        }
    }
}