using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Persistence.Configurations;

public class OrderItemConfiguration: IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasIndex(oi => oi.OrderId);

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(oi => oi.ProductSnapshot)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(oi => oi.UnitPrice)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(oi => oi.TotalPrice)
            .HasColumnType("numeric(18,2)")
            .IsRequired();
    }
}