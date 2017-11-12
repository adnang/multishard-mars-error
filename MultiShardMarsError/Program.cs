using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiShardMarsError
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var program = new Program();

            var threads = Enumerable.Range(0, 150).Select(
                i => new Thread(
                    () =>
                    {
                        while (true)
                        {
                            try
                            {
                                program.ExecuteWithRetries().GetAwaiter().GetResult();
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

        private async Task<int> ExecuteWithRetries()
        {
            var multiShardCommandExecutor = new MultiShardCommandExecutor(new MultiShardConnectionFactory());
            return await new RetryingMultiShardCommandExecutor(multiShardCommandExecutor)
                .Execute(new HealthcheckDatabaseQuery().CommandFunc(new object()));
        }
    }
}
