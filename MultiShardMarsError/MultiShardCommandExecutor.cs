using System;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.Query;

namespace MultiShardMarsError
{
    public class MultiShardCommandExecutor : IMultiShardCommandExecutor
    {
        private readonly MultiShardConnectionFactory _multiShardConnectionFactory;

        public MultiShardCommandExecutor(MultiShardConnectionFactory multiShardConnectionFactory)
        {
            _multiShardConnectionFactory = multiShardConnectionFactory;
        }

        public async Task<TResult> Execute<TResult>(Func<MultiShardConnection, Task<TResult>> commandFunc)
        {
            using (var databaseConnection = _multiShardConnectionFactory.CreateMultiShardConnection())
            {
                return await commandFunc(databaseConnection);
            }
        }
    }
}