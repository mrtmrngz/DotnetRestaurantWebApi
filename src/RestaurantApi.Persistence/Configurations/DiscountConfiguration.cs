using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Persistence.Configurations;

public class DiscountConfiguration: IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("discounts");

        builder.HasOne(d => d.Product)
            .WithMany(p => p.Discounts)
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.ProductId, d.IsActive })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
    }
}