namespace Resource.Application.Models.User
{
    public class UserModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
