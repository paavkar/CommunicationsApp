using CommunicationsApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CommunicationsApp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Server> Servers { get; set; } = null!;
        public DbSet<ChannelClass> ChannelClasses { get; set; } = null!;
        public DbSet<Channel> Channels { get; set; } = null!;
        public DbSet<ServerRole> ServerRoles { get; set; } = null!;
        public DbSet<ServerProfile> ServerProfiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ChatMessage>()
                .Ignore(c => c.Reactions);
        }
    }
}
