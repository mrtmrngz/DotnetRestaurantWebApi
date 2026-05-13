using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Configurations;

public class AppRoleConfiguration: IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("Roles");
        
        builder.HasKey(r => r.Id);
 
        builder.Property(r => r.Name)
            .HasMaxLength(256);
 
        builder.Property(r => r.NormalizedName)
            .HasMaxLength(256);
 
        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasName("IX_Roles_NormalizedName");
    }
}