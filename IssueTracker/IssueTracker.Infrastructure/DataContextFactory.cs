using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IssueTracker.Infrastructure.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(String[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();

            // Usiamo la stessa identica stringa di connessione locale
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=IssueTrackerDb_Clean;Trusted_Connection=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString, b => b.MigrationsAssembly("IssueTracker.Infrastructure"));

            return new DataContext(optionsBuilder.Options);
        }
    }
}