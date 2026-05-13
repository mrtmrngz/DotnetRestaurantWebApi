using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Configurations;

public class AppUserConfiguration: IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users");
        
        builder.HasKey(u => u.Id);
 
        builder.Property(u => u.Name)
            .HasMaxLength(100)
            .IsRequired();
 
        builder.Property(u => u.Surname)
            .HasMaxLength(100)
            .IsRequired();
 
        builder.Property(u => u.Email)
            .HasMaxLength(256);
 
        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(256);
 
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasName("IX_Users_Email");
 
        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false);
    }
}