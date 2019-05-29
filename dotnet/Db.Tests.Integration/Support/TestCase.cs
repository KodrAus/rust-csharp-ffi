using System;
using System.Threading.Tasks;
using Db.Tests.Integration.Api;

namespace Db.Tests.Integration.Support
{
    sealed class TestCase
    {
        private readonly Client _client;
        private readonly ServerProcess _server;
        private readonly ITestCase _test;

        public TestCase(ITestCase test, ServerProcess server, Client client)
        {
            _test = test;
            _server = server;
            _client = client;
        }

        public string ServerOutput => _server.Output;
        public string Name => _test.GetType().FullName;

        public async Task Execute()
        {
            // Give the server time to start up
            // If it isn't running then fail
            await Task.Delay(TimeSpan.FromSeconds(5));
            if (_server.HasExited) throw new Exception("The server process has already exited");

            await _test.Execute(_client);
        }
    }
}