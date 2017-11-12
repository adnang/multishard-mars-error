using Microsoft.Azure.SqlDatabase.ElasticScale.Query;

namespace MultiShardMarsError
{
    public interface IMultiShardConnectionFactory
    {
        MultiShardConnection CreateMultiShardConnection();
    }
}