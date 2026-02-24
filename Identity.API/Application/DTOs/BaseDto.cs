namespace Identity.API.Application.DTOs;

public abstract class BaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
