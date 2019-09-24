using Microsoft.EntityFrameworkCore;

namespace Joonasw.ManagedIdentityDemos.Data
{
    public class MsiDbContext : DbContext
    {
        public MsiDbContext(DbContextOptions<MsiDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestModel> Tests { get; set; }
    }
}
