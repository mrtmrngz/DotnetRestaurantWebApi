using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Configurations;

public class CartConfiguration: IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");
        
        // cart items relation
        builder.HasMany(c => c.CartItems)
            .WithOne(ci => ci.Cart)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // restaurant table relation
        builder.HasOne(c => c.RestaurantTable)
            .WithMany()
            .HasForeignKey(c => c.RestaurantTableId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // user relation
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // indexes
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.RestaurantTableId);
    }
}