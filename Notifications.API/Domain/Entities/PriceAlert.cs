using System;

namespace Notifications.API.Domain.Entities
{
    public class PriceAlert : BaseEntity
    {
        public Guid UserId { get; private set; }

        public string Symbol { get; private set; } = string.Empty;

        public decimal TargetPrice { get; private set; }

        public bool IsAbove { get; private set; }

        public bool IsActive { get; private set; }

        public DateTime CreatedAt { get; private set; }

        protected PriceAlert() { }

        public PriceAlert(Guid userId, string symbol, decimal targetPrice, bool isAbove)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID could not be empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol could not be empty.", nameof(symbol));

            if (targetPrice <= 0)
                throw new ArgumentException("Target price must be bigger than 0.", nameof(targetPrice));

            UserId = userId;
            Symbol = symbol.ToUpperInvariant().Trim();
            TargetPrice = targetPrice;
            IsAbove = isAbove;

            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
            }
        }
    }
}

