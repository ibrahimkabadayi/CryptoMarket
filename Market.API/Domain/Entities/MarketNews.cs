namespace Market.API.Domain.Entities;

public class MarketNews : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<string> RelatedSymbols { get; set; } = [];
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
