using Microsoft.EntityFrameworkCore;
using Nepal.Payments.Gateways.Demo.Models;

namespace Nepal.Payments.Gateways.Demo.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<FonepayTransaction> FonepayTransactions => Set<FonepayTransaction>();
}