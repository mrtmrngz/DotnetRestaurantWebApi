namespace RestaurantApi.Domain.Entities;

public class RestaurantTable
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}