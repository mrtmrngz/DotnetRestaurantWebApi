using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Persistence.Configurations;

public class ProductMediaConfiguration: IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        builder.ToTable("product_media");

        builder.HasOne(pm => pm.Product)
            .WithMany(p => p.ProductMedias)
            .HasForeignKey(pm => pm.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.Media)
            .WithMany()
            .HasForeignKey(pm => pm.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pm => new { pm.ProductId, pm.MediaId }).IsUnique();
    }
}