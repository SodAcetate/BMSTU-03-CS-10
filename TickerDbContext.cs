using Microsoft.EntityFrameworkCore;

namespace Lab_10_1{
    public partial class TickersDbContext : DbContext
    {
        public TickersDbContext() : base(){
            Database.EnsureCreated();
        }
        public TickersDbContext(DbContextOptions<TickersDbContext> options) : base(options){
            Database.EnsureCreated();
        }
        public DbSet<TickerEntry> Tickers { get; set; }
        public DbSet<PriceEntry> Prices { get; set; }
        public DbSet<TodaysConditionEntry> TodaysConditions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=sql1;User Id=sa;Password=VeryG00dPassword;TrustServerCertificate=true");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }
    
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    
    }
}