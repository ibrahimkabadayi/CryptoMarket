namespace Identity.API.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
