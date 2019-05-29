using Autofac;
using Db.Tests.Integration.Support;

namespace Db.Tests.Integration
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new TestCaseRunnerModule(args[0]));

            using var container = builder.Build();
            return container.Resolve<TestCaseRunner>().Run();
        }
    }
}