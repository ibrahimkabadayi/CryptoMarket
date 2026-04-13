namespace Notifications.API.Models
{
    public class CreatePriceAlertRequest
    {
        public Guid UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public decimal TargetPrice { get; set; }
        public bool IsAbove { get; set; }
    }
}
