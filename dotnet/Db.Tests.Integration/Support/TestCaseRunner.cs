using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.OwnedInstances;
using Db.Tests.Integration.Support;

namespace Db.Tests.Integration.Support
{
    class TestCaseRunner
    {
        readonly IEnumerable<Func<Owned<TestCase>>> _tests;

        public TestCaseRunner(IEnumerable<Func<Owned<TestCase>>> tests)
        {
            _tests = tests;
        }

        public int Run()
        {
            var results = new ConcurrentBag<string>();
            var success = true;

            // Randomize the order of the test cases
            var testsToRun = _tests.OrderBy(_ => Guid.NewGuid());

            Parallel.ForEach(testsToRun, new ParallelOptions {MaxDegreeOfParallelism = 4},
                (testFactory, state) =>
                {
                    using (var test = testFactory())
                    {
                        try
                        {
                            test.Value.Execute().GetAwaiter().GetResult();
                            results.Add($"PASS {test.Value.Name}\n{test.Value.ServerOutput}");
                        }
                        catch (Exception e)
                        {
                            success = false;
                            results.Add($"FAIL {test.Value.Name}\n{e}\n{test.Value.ServerOutput}");
                        }
                    }
                });

            foreach (var result in results) Console.WriteLine(result);

            return success ? 0 : 1;
        }
    }
}