using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace KnowledgeBank.Models;

public class User : IdentityUser
{
    // Add more fields here if needed
}

public class UpdateEmailDto
{
    public required string UserId { get; set; }

    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Incorrect email format.")]  
    public required string Email { get; set; }
}

public class SignUpDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Token { get; set; }
}