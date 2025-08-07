using GameRash.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRash.Data
{
    public class GameRashDbContext : DbContext
{
    public GameRashDbContext(DbContextOptions<GameRashDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Developer> Developers { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameReview> GameReviews { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Library> Libraries { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships and constraints

        // User-Developer (One-to-One)
        modelBuilder.Entity<Developer>()
            .HasOne(d => d.User)
            .WithOne(u => u.Developer)
            .HasForeignKey<Developer>(d => d.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // User-Admin (One-to-One)
        modelBuilder.Entity<Admin>()
            .HasOne(a => a.User)
            .WithOne(u => u.Admin)
            .HasForeignKey<Admin>(a => a.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // Developer-Game (One-to-Many)
        modelBuilder.Entity<Game>()
            .HasOne(g => g.Developer)
            .WithMany(d => d.Games)
            .HasForeignKey(g => g.DeveloperID)
            .OnDelete(DeleteBehavior.Restrict);

        // User-GameReview (One-to-Many)
        modelBuilder.Entity<GameReview>()
            .HasOne(gr => gr.User)
            .WithMany(u => u.GameReviews)
            .HasForeignKey(gr => gr.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // Game-GameReview (One-to-Many)
        modelBuilder.Entity<GameReview>()
            .HasOne(gr => gr.Game)
            .WithMany(g => g.GameReviews)
            .HasForeignKey(gr => gr.GameID)
            .OnDelete(DeleteBehavior.Cascade);

        // User-Purchase (One-to-Many)
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.User)
            .WithMany(u => u.Purchases)
            .HasForeignKey(p => p.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // Game-Purchase (One-to-Many)
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.Game)
            .WithMany(g => g.Purchases)
            .HasForeignKey(p => p.GameID)
            .OnDelete(DeleteBehavior.Restrict);

        // Purchase-Payment (One-to-Many)
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Purchase)
            .WithMany(pr => pr.Payments)
            .HasForeignKey(p => p.PurchaseID)
            .OnDelete(DeleteBehavior.Cascade);

        // User-Library (One-to-Many)
        modelBuilder.Entity<Library>()
            .HasOne(l => l.User)
            .WithMany(u => u.Libraries)
            .HasForeignKey(l => l.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // Game-Library (One-to-Many)
        modelBuilder.Entity<Library>()
            .HasOne(l => l.Game)
            .WithMany(g => g.Libraries)
            .HasForeignKey(l => l.GameID)
            .OnDelete(DeleteBehavior.Restrict);

        // User-Wishlist (One-to-Many)
        modelBuilder.Entity<Wishlist>()
            .HasOne(w => w.User)
            .WithMany(u => u.Wishlists)
            .HasForeignKey(w => w.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // Game-Wishlist (One-to-Many)
        modelBuilder.Entity<Wishlist>()
            .HasOne(w => w.Game)
            .WithMany(g => g.Wishlists)
            .HasForeignKey(w => w.GameID)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite unique indexes to prevent duplicates
        modelBuilder.Entity<GameReview>()
            .HasIndex(gr => new { gr.UserID, gr.GameID })
            .IsUnique();

        modelBuilder.Entity<Library>()
            .HasIndex(l => new { l.UserID, l.GameID })
            .IsUnique();

        modelBuilder.Entity<Wishlist>()
            .HasIndex(w => new { w.UserID, w.GameID })
            .IsUnique();

        // Seed data (optional)
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Users
        modelBuilder.Entity<User>().HasData(
            new User { UserID = 1, Username = "admin_user", Password = "hashedpassword123", Email = "admin@gamingplatform.com" },
            new User { UserID = 2, Username = "dev_studio", Password = "hashedpassword456", Email = "dev@studio.com" },
            new User { UserID = 3, Username = "gamer_one", Password = "hashedpassword789", Email = "gamer@email.com" }
        );

        // Seed Admin
        modelBuilder.Entity<Admin>().HasData(
            new Admin { AdminID = 1, UserID = 1 }
        );

        // Seed Developer
        modelBuilder.Entity<Developer>().HasData(
            new Developer { DeveloperID = 1, UserID = 2, StudioName = "Awesome Games Studio", Bio = "We create amazing indie games!" }
        );

        // Seed Games
        modelBuilder.Entity<Game>().HasData(
            new Game
            {
                GameID = 1,
                DeveloperID = 1,
                Title = "Epic Adventure",
                Description = "An amazing RPG adventure game",
                CoverImage = "epic_adventure_cover.jpg"
            },
            new Game
            {
                GameID = 2,
                DeveloperID = 1,
                Title = "Space Explorer",
                Description = "Explore the vast universe",
                CoverImage = "space_explorer_cover.jpg"
            }
        );
    }
}
}
