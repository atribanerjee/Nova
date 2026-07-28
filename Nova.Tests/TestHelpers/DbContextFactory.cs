using Microsoft.EntityFrameworkCore;
using Nova.DB;

namespace Nova.Tests.TestHelpers
{
    internal static class DbContextFactory
    {
        public static NovaDBContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<NovaDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new NovaDBContext(options);
        }
    }
}
