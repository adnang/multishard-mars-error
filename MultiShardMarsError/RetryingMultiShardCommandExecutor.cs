using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.Query;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace MultiShardMarsError
{
    public class RetryingMultiShardCommandExecutor : IMultiShardCommandExecutor
    {
        private readonly IMultiShardCommandExecutor _innerExecutor;

        public RetryingMultiShardCommandExecutor(IMultiShardCommandExecutor innerExecutor)
        {
            _innerExecutor = innerExecutor;
        }

        public async Task<TResult> Execute<TResult>(Func<MultiShardConnection, Task<TResult>> commandFunc)
        {
            var retryPolicy = CreateIncrementalPolicy();
            return await retryPolicy.ExecuteAsync(() => _innerExecutor.Execute(commandFunc));
        }

        private static RetryPolicy CreateIncrementalPolicy()
        {
            const int retryCount = 3;
            var initialInterval = TimeSpan.FromMilliseconds(10);
            var increment = TimeSpan.FromMilliseconds(200);

            var incrementalRetryStrategy = new Incremental(null, retryCount, initialInterval, increment, true);

            return new RetryPolicy(new SqlDatabaseTransientErrorDetectionStrategy(), incrementalRetryStrategy);
        }

    }
}