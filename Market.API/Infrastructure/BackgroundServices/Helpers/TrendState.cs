namespace Market.API.Infrastructure.BackgroundServices.Helpers;

public class TrendState
{
    private decimal _momentum = 0;
    private int _remainingTicks = 0;

    private const decimal BaseVolatility = 0.0008m;
    private const decimal MomentumFactor = 0.6m;

    public TrendState(Random rng)
    {
        ResetTrend(rng);
    }

    public decimal NextPrice(decimal currentPrice, Random rng)
    {
        if (--_remainingTicks <= 0)
            ResetTrend(rng);

        var noise = (decimal)(rng.NextDouble() * 2 - 1) * BaseVolatility;

        var momentumEffect = _momentum * MomentumFactor;

        var spike = 0m;
        if (rng.NextDouble() < 0.002)
            spike = (decimal)(rng.NextDouble() * 2 - 1) * BaseVolatility * 5;

        var totalChange = noise + momentumEffect + spike;

        var newPrice = Math.Max(currentPrice * (1 + totalChange), 0.0001m);

        return Math.Round(newPrice, 4);
    }

    private void ResetTrend(Random rng)
    {
        var direction = rng.NextDouble();
        _momentum = direction switch
        {
            < 0.35 => (decimal)(rng.NextDouble() * 0.0003),
            < 0.70 => -(decimal)(rng.NextDouble() * 0.0003),
            _ => (decimal)(rng.NextDouble() * 0.0001 - 0.00005)
        };

        _remainingTicks = rng.Next(20, 200);
    }
}