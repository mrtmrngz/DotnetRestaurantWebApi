namespace RestaurantApi.Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; }
    public int GuestCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Status { get; set; } = ReservationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
    public bool IsDeleted { get; set; }

    public Guid RestaurantTableId { get; set; }
    public RestaurantTable RestaurantTable { get; set; } = null!;

    public Guid? UserId { get; set; }
}

public class ReservationStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Completed = "Completed";
    public const string Canceled = "Canceled";
}