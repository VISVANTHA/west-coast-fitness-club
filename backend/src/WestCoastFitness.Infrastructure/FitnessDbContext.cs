using System.Text;
using Microsoft.EntityFrameworkCore;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Infrastructure;

/// <summary>
/// EF Core model. Table and column names are explicit snake_case so they match
/// database/schema/001-initial-schema.sql exactly.
/// </summary>
public sealed class FitnessDbContext(DbContextOptions<FitnessDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();

    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();

    public DbSet<Trainer> Trainers => Set<Trainer>();

    public DbSet<FitnessClass> Classes => Set<FitnessClass>();

    public DbSet<ClassBooking> ClassBookings => Set<ClassBooking>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    internal static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<MembershipPlan>(e =>
        {
            e.ToTable("membership_plans");
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.MonthlyPrice).HasPrecision(10, 2);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Trainer>(e =>
        {
            e.ToTable("trainers");
            e.Property(x => x.FullName).HasMaxLength(120);
            e.Property(x => x.Specialty).HasMaxLength(120);
        });

        modelBuilder.Entity<FitnessClass>(e =>
        {
            e.ToTable("classes");
            e.Property(x => x.Title).HasMaxLength(120);
            e.Property(x => x.Location).HasMaxLength(80);
            e.HasOne<Trainer>().WithMany().HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.StartsAt);
        });

        modelBuilder.Entity<Member>(e =>
        {
            e.ToTable("members");
            e.Property(x => x.FirstName).HasMaxLength(80);
            e.Property(x => x.LastName).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.HasOne<MembershipPlan>().WithMany().HasForeignKey(x => x.MembershipPlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<UserAccount>(e =>
        {
            e.ToTable("user_accounts");
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.PasswordHash).HasMaxLength(500);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<ClassBooking>(e =>
        {
            e.ToTable("class_bookings");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FitnessClass).WithMany().HasForeignKey(x => x.FitnessClassId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.FitnessClassId })
                .IsUnique()
                .HasFilter("status = 'Confirmed'")
                .HasDatabaseName("ux_class_bookings_active_member_class");
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.Reference).HasMaxLength(80);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
        });

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            property.SetColumnName(ToSnakeCase(property.Name));
        }
    }
}
