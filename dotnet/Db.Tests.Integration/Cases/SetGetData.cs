using System;
using System.Threading.Tasks;
using Db.Tests.Integration.Api;
using Db.Tests.Integration.Support;
using Xunit;

namespace Db.Tests.Integration.Cases
{
    class SetGetData : ITestCase
    {
        public async Task Execute(Client client)
        {
            var id = "testdocs-1";
            var value = Guid.NewGuid().ToString();

            await client.Set(new Data(id, new {value}));

            var created = await client.GetAll();

            Assert.Equal(1, created.Length);
            Assert.Equal(value, (string) created[0].DynamicValue.value);
        }
    }
}