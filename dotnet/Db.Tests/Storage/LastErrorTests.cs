using System;
using Db.Storage.Native;
#if DEBUG
using Xunit;

namespace Db.Tests.Storage
{
    public class LastResultTests
    {
        [Fact]
        public void NativeErrorsBecomeExceptions()
        {
            var nativeException = Assert.Throws<Exception>(() => Bindings.db_test_error());

            Assert.Equal("Native storage failed (InternalError), A test error.", nativeException.Message);
        }

        [Fact]
        public void NativeErrorsUseDefaultMessageWhenLastResultChangesToNewError()
        {
            var nativeResult = Bindings.db_test_error(false);
            Bindings.db_test_error(false);

            var nativeException = Assert.Throws<Exception>(() => nativeResult.Check());
            Assert.Equal("Native storage failed with InternalError", nativeException.Message);
        }

        [Fact]
        public void NativeErrorsUseDefaultMessageWhenLastResultChangesToOk()
        {
            var nativeResult = Bindings.db_test_error(false);
            Bindings.db_test_ok();

            var nativeException = Assert.Throws<Exception>(() => nativeResult.Check());
            Assert.Equal("Native storage failed with InternalError", nativeException.Message);
        }
    }
}
#endif