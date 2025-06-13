using CommunicationsApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CommunicationsApp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Server> Servers { get; set; } = null!;
        public DbSet<Channel> Channels { get; set; } = null!;
        public DbSet<ServerRole> ServerRoles { get; set; } = null!;
        public DbSet<ServerProfile> ServerProfiles { get; set; } = null!;
        public DbSet<UserServerRole> UserServerRoles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CHANNEL configuration: Each Channel belongs to one Server.
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity
                    .HasOne<Server>()                 // a Channel is related to one Server
                    .WithMany(s => s.Channels)         // a Server can have many Channels
                    .HasForeignKey(e => e.ServerId)    // foreign key on Channel
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SERVER configuration: The Server entity is the principal for Channels and ServerRoles.
            modelBuilder.Entity<Server>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            // SERVERROLE configuration: Each ServerRole belongs to one Server.
            modelBuilder.Entity<ServerRole>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity
                    .HasOne<Server>()
                    .WithMany(s => s.Roles)
                    .HasForeignKey(e => e.ServerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SERVERPROFILE configuration: Composite key and relationships.
            modelBuilder.Entity<ServerProfile>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Link ServerProfile to Server.
                entity
                    .HasOne<Server>()
                    .WithMany()
                    .HasForeignKey(e => e.ServerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserServerRole>()
                .HasKey(usr => new { usr.UserId, usr.ServerId, usr.RoleId });

            modelBuilder.Entity<UserServerRole>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(usr => usr.UserId);

            modelBuilder.Entity<UserServerRole>()
                .HasOne<Server>()
                .WithMany()
                .HasForeignKey(usr => usr.ServerId);

            modelBuilder.Entity<UserServerRole>()
                .HasOne<ServerRole>()
                .WithMany()
                .HasForeignKey(usr => usr.RoleId);
        }
    }
}
