using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Persistence.Identity;

namespace RestaurantApi.Persistence.Configurations;

public class OrderConfiguration: IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.SubTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.Total)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.ShippingCost)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.AddressSnapshot)
            .IsRequired()
            .HasColumnType("text");

        // enums
        builder.Property(o => o.OrderStatus)
            .HasConversion<int>()
            .HasDefaultValue(OrderStatus.Pending);

        builder.Property(o => o.PaymentStatus)
            .HasConversion<int>()
            .HasDefaultValue(PaymentStatus.Pending);
        
        // order items relation
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // user relation
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // table relation
        builder.HasOne<RestaurantTable>()
            .WithMany()
            .HasForeignKey(o => o.RestaurantTableId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // user index
        builder.HasIndex(o => new { o.UserId, o.OrderDate });
    }
}