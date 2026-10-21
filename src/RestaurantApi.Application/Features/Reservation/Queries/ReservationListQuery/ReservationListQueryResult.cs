namespace RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;

public record ReservationListQueryResult()
{
    public Guid Id { get; set; }
    public int GuestCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Status { get; set; }
    public string? Note { get; set; }
    public int RestaurantTableNumber { get; set; }
};