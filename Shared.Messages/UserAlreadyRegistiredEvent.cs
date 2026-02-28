namespace Shared.Messages;

public record UserAlreadyRegistiredEvent
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}
