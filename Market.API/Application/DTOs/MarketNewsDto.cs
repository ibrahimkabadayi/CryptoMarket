namespace Market.API.Application.DTOs;

public class MarketNewsDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<string> RelatedSymbols { get; set; } = [];
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
