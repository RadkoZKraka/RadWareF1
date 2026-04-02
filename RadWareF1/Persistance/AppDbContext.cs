using Microsoft.EntityFrameworkCore;
using RadWareF1.Domain;

namespace RadWareF1.Persistance;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.ToTable("Users");

            entity.Property(x => x.Email)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.ToTable("Profiles");

            entity.HasOne(x => x.User)
                .WithOne(x => x.Profile)
                .HasForeignKey<Profile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserId)
                .IsUnique();
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.ToTable("Groups");

            entity.Property(x => x.Name)
                .IsRequired();

            entity.Property(x => x.JoinCodeHash)
                .IsRequired();

            entity.HasIndex(x => x.JoinCodeHash)
                .IsUnique();
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(x => new { x.GroupId, x.UserId });

            entity.ToTable("GroupMembers");

            entity.Property(x => x.Role)
                .HasConversion<int>()
                .IsRequired();

            entity.HasOne(x => x.Group)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.GroupMembers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}