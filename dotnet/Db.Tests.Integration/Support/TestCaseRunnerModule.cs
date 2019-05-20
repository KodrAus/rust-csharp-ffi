using Autofac;
using Db.Tests.Integration.Api;

namespace Db.Tests.Integration.Support
{
    public class TestCaseRunnerModule : Module
    {
        private readonly string _binPath;

        public TestCaseRunnerModule(string binPath)
        {
            _binPath = binPath;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(ThisAssembly)
                .As<ITestCase>();

            builder.RegisterType<TestCaseRunner>();

            builder.RegisterType<DataPath>().InstancePerOwned<TestCase>();
            builder.RegisterType<ListenUrl>().InstancePerOwned<TestCase>();

            builder.Register(ctx =>
                    new ServerProcess(_binPath, ctx.Resolve<ListenUrl>(), ctx.Resolve<DataPath>()))
                .As<ServerProcess>()
                .InstancePerOwned<TestCase>();

            builder.Register(ctx => new Client(ctx.Resolve<ListenUrl>()))
                .As<Client>()
                .InstancePerOwned<TestCase>();

            builder.RegisterAdapter<ITestCase, TestCase>((ctx, test) =>
                new TestCase(test, ctx.Resolve<ServerProcess>(), ctx.Resolve<Client>()));
        }
    }
}