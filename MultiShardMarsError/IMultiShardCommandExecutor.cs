using System;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.Query;

namespace MultiShardMarsError
{
    public interface IMultiShardCommandExecutor
    {
        Task<TResult> Execute<TResult>(Func<MultiShardConnection, Task<TResult>> commandFunc);
    }
}