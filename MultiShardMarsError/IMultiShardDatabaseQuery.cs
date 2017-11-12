using System;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.Query;

namespace MultiShardMarsError
{
    public interface IMultiShardDatabaseQuery<in TCriteria, TResult>
    {
        Func<MultiShardConnection, Task<TResult>> CommandFunc(TCriteria criteria);
    }
}