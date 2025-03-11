using Microsoft.EntityFrameworkCore;
using Nova.DB.POCO;

namespace Nova.DB
{
    public class NovaDBContext: DbContext
    {
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
    }
}
