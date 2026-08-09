using System.ComponentModel.DataAnnotations;

namespace Dwuma.Models.Authentication;

public sealed class LoginRequest
{
    [Required]
    [MaxLength(255)]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}