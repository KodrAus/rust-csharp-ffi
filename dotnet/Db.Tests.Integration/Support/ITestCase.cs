using System.Threading.Tasks;
using Db.Tests.Integration.Api;

namespace Db.Tests.Integration.Support
{
    interface ITestCase
    {
        Task Execute(Client client);
    }
}