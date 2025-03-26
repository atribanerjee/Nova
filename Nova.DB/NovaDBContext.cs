using Microsoft.EntityFrameworkCore;
using Nova.DB.POCO;

namespace Nova.DB
{
    public class NovaDBContext: DbContext
    {
        public NovaDBContext(DbContextOptions<NovaDBContext> options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<UserActivities> UserActivities { get; set; }
    }
}
