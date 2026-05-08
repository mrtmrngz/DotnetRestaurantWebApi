using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Persistence.Configurations;

public class RestaurantTableConfiguration: IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("restaurant_tables");
        
        // reservation relation
        builder.HasMany(t => t.Reservations)
            .WithOne(r => r.RestaurantTable)
            .HasForeignKey(r => r.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}