using System.ComponentModel;
using System.Configuration;
using System.Threading.Tasks;

namespace MultiShardMarsError
{
    public interface IDatabaseQueryExecutor
    {
        Task<TResult> ExecuteQuery<TCriteria, TResult>(TCriteria criteria)
            where TCriteria : ISpecifyBagOrigin;
    }

    public class MultiShardDatabaseQueryExecutor : IDatabaseQueryExecutor
    {
        private readonly IContainer _container;

        public MultiShardDatabaseQueryExecutor(IContainer container)
        {
            _container = container;
        }

        public async Task<TResult> ExecuteQuery<TCriteria, TResult>(TCriteria criteria)
            where TCriteria : ISpecifyBagOrigin
        {
            IMultiShardCommandExecutor commandExecutor = null;
            IMultiShardDatabaseQuery<TCriteria, TResult> query = null;
            
            try
            {
                commandExecutor = _container.Resolve<IMultiShardCommandExecutor>();
                query = _container.Resolve<IMultiShardDatabaseQuery<TCriteria, TResult>>();
                return await commandExecutor.Execute(query.CommandFunc(criteria));
            }
            finally
            {
                _container.Release(commandExecutor);
                _container.Release(query);
            }
        }
    }

    public class OriginSwitchingMutliShardDatabaseQueryExecutor : IDatabaseQueryExecutor
    {
        private readonly IDatabaseQueryExecutor _innerExecutor;
        private IContainer _container;

        public OriginSwitchingMutliShardDatabaseQueryExecutor(IDatabaseQueryExecutor innerExecutor, IContainer container)
        {
            _innerExecutor = innerExecutor;
            _container = container;
        }

        public async Task<TResult> ExecuteQuery<TCriteria, TResult>(TCriteria criteria)
            where TCriteria : ISpecifyBagOrigin
        {
            if (criteria.BagOrigin.Equals(ConfigurationManager.AppSettings["ThisRegion.BagOrigin"]))
            {
                return await _innerExecutor.ExecuteQuery<TCriteria, TResult>(criteria);
            }
            else
            {
                IMultiShardCommandExecutor commandExecutor = null;
                IMultiShardDatabaseQuery<TCriteria, TResult> query = null;
            
                try
                {
                    commandExecutor = _container.Resolve<IMultiShardCommandExecutor>("SecondaryRegionMultiShardCommandExecutor");
                    query = _container.Resolve<IMultiShardDatabaseQuery<TCriteria, TResult>>();
                    return await commandExecutor.Execute(query.CommandFunc(criteria));
                }
                finally
                {
                    _container.Release(commandExecutor);
                    _container.Release(query);
                }
            }
        }
    }

    public interface ISpecifyBagOrigin
    {
        string BagOrigin { get; }
    }

    public interface IContainer
    {
        T Resolve<T>(string name = null);

        void Release<T>(T t);
    }
}