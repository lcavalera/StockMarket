using Bourse.Models;
using Microsoft.EntityFrameworkCore;

namespace Bourse.Data
{
    public class BourseContext : DbContext
    {
        public BourseContext(DbContextOptions<BourseContext> options) : base(options) { }

        public DbSet<Indice> Indices { get; set; }
        //public DbSet<RealPrice> RealPrices { get; set; }
        public DbSet<StockData> StockDatas { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<RealPrice>().HasKey(h => new { h.Id, h.IndiceId,});
            modelBuilder.Entity<StockData>().ToTable("StockData");
        }
    }
}
