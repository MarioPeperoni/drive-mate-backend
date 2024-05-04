using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using Drive_Mate_Server.Models;

namespace Drive_Mate_Server.Data
{
    public class MyDbContext : DbContext
    {
        public DbSet<User> Users { get; init; }
        public DbSet<Ride> Rides { get; init; }
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().ToCollection("Users");
            modelBuilder.Entity<Ride>().ToCollection("Rides");
        }
    }
}
