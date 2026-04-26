namespace Identity.API.Application.DTOs;

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string Token { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
