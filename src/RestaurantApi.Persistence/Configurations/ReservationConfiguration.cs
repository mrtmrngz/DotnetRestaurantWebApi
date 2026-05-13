using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Configurations;

public class ReservationConfiguration: IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.Property(r => r.FullName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.PhoneNumber).HasMaxLength(15).IsRequired();
        builder.Property(r => r.Email).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Note).HasMaxLength(350).IsRequired();

        builder.HasQueryFilter(x => !x.IsDeleted);
        
        // table relations
        builder.HasOne(r => r.RestaurantTable)
            .WithMany(rt => rt.Reservations)
            .HasForeignKey(r => r.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // User relation
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // index
        builder.HasIndex(r => new { r.RestaurantTableId, r.EndTime, r.StartTime });

    }
}