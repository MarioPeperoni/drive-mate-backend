using Microsoft.EntityFrameworkCore;
using Drive_Mate_Server.Models;

namespace Drive_Mate_Server.Data
{
    public class MyDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Passenger>()
                .HasKey(p => new { p.UserId, p.RideId });

            modelBuilder.Entity<Passenger>()
                .HasOne(p => p.User)
                .WithMany(u => u.RidesAsPassenger)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<Passenger>()
                .HasOne(p => p.Ride)
                .WithMany(r => r.Passengers)
                .HasForeignKey(p => p.RideId);

            modelBuilder.Entity<Ride>()
                .HasOne(r => r.Driver)
                .WithMany(u => u.RidesAsDriver)
                .HasForeignKey(r => r.UserId);
        }

    }
}
