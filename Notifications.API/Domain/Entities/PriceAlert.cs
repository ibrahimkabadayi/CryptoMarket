namespace Notifications.API.Domain.Entities;

public class PriceAlert : BaseEntity
{
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public double TargetPrice { get; set; }
    public bool IsAbove { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
