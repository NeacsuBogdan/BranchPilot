using BranchPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BranchPilot.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(tenant => tenant.Slug)
            .HasMaxLength(140)
            .IsRequired();

        builder.HasIndex(tenant => tenant.Slug)
            .IsUnique();
    }
}

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(location => location.Id);

        builder.Property(location => location.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(location => location.Code)
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(location => location.TimeZone)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(location => location.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(location => new { location.TenantId, location.Code })
            .IsUnique();
    }
}

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(user => user.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique();
    }
}

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");

        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(membership => new { membership.TenantId, membership.UserId })
            .IsUnique();
    }
}

internal sealed class MembershipLocationConfiguration : IEntityTypeConfiguration<MembershipLocation>
{
    public void Configure(EntityTypeBuilder<MembershipLocation> builder)
    {
        builder.ToTable("MembershipLocations");

        builder.HasKey(assignment => new { assignment.MembershipId, assignment.LocationId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(assignment => assignment.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Membership>()
            .WithMany()
            .HasForeignKey(assignment => assignment.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(assignment => assignment.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(assignment => new { assignment.TenantId, assignment.LocationId });
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(token => token.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(token => new { token.TenantId, token.UserId });
    }
}
