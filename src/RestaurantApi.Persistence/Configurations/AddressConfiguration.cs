using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Configurations;

public class AddressConfiguration: IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");
        
        builder.Ignore(x => x.FullAddress);
        builder.HasKey(x => x.Id);
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.UserId, x.IsDefault })
            .IsUnique()
            .HasFilter("\"IsDefault\" = true");

        builder.Property(x => x.Title).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.City).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Town).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Neighborhood).HasMaxLength(70).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(85).IsRequired();
        builder.Property(x => x.BuildingInfo).HasMaxLength(150).IsRequired(false);
        builder.Property(x => x.BuildingNumber).HasMaxLength(15).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(15).IsRequired();
    }
}