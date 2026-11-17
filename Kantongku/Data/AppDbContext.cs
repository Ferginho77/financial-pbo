using Kantongku.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kantongku.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Pocket> Pockets => Set<Pocket>();
    public DbSet<PocketMember> PocketMembers => Set<PocketMember>();
    public DbSet<ExpenseGroup> ExpenseGroups => Set<ExpenseGroup>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        });

        builder.Entity<Pocket>(e =>
        {
            e.ToTable("Pockets");
            e.Property(p => p.Name).IsRequired().HasMaxLength(80);
            e.Property(p => p.Description).HasMaxLength(255);
            e.Property(p => p.Color).IsRequired().HasMaxLength(20);
            e.Property(p => p.Balance).HasColumnType("decimal(18,2)");
            e.Property(p => p.TargetAmount).HasColumnType("decimal(18,2)");

            e.HasOne(p => p.Owner)
                .WithMany(u => u.OwnedPockets)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(p => p.OwnerId);
        });

        builder.Entity<PocketMember>(e =>
        {
            e.ToTable("PocketMembers");
            e.Property(m => m.Role).HasConversion<int>();

            e.HasOne(m => m.Pocket)
                .WithMany(p => p.Members)
                .HasForeignKey(m => m.PocketId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.User)
                .WithMany(u => u.PocketMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Satu user hanya boleh satu kali menjadi anggota di sebuah kantong.
            e.HasIndex(m => new { m.PocketId, m.UserId }).IsUnique();
        });

        builder.Entity<ExpenseGroup>(e =>
        {
            e.ToTable("ExpenseGroups");
            e.Property(g => g.Name).IsRequired().HasMaxLength(60);

            e.HasOne(g => g.User)
                .WithMany(u => u.ExpenseGroups)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(g => g.UserId);
        });

        builder.Entity<ExpenseCategory>(e =>
        {
            e.ToTable("ExpenseCategories");
            e.Property(c => c.Name).IsRequired().HasMaxLength(60);

            e.HasOne(c => c.Group)
                .WithMany(g => g.Categories)
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Transaction>(e =>
        {
            e.ToTable("Transactions");
            e.Property(t => t.Type).HasConversion<int>();
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Title).IsRequired().HasMaxLength(120);
            e.Property(t => t.Source).HasMaxLength(100);
            e.Property(t => t.Note).HasMaxLength(255);
            e.Property(t => t.Date).HasColumnType("date");
            e.Property(t => t.GroupName).HasMaxLength(60);
            e.Property(t => t.CategoryName).HasMaxLength(60);
            e.Property(t => t.RelatedPocketName).HasMaxLength(80);
            e.Ignore(t => t.IsCredit);

            e.HasOne(t => t.Pocket)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.PocketId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Kategori dihapus -> transaksi tetap ada (snapshot nama tersimpan).
            e.HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => new { t.PocketId, t.Date });
            e.HasIndex(t => t.TransferGroupId);
        });
    }
}
