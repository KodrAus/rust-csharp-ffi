using System.Threading.Tasks;
using Db.Tests.Integration.Api;

namespace Db.Tests.Integration.Support
{
    internal interface ITestCase
    {
        Task Execute(Client client);
    }
}