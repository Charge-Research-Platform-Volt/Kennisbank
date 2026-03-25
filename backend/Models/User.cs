using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace KnowledgeBank.Models;

public class User : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;

    // Technically able to hit integer limit, practically impossible.
    // Increments when the user selects a new custom avatar.
    public int CustomAvatarVersion { get; set; } = 0;

    public bool HasCustom { get; set; }

    [Obsolete("Only for EF Core and Identity or testing without a real database. Use the parameterized constructor instead.")]
    public User() { } // Default constructor for EF Core

    public User(string firstName, string lastName, string email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        UserName = email;
    }
}

public class UpdateEmailDto
{
    public required string UserId { get; set; }

    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Incorrect email format.")]  
    public required string Email { get; set; }
}

public class SignUpDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Incorrect email format.")]  
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Token { get; set; }
    public IFormFile? Avatar { get; set; }
}

public class UpdateUserDto
{
    public string? NewFirstName { get; set; }
    public string? NewLastName { get; set; }
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Incorrect email format.")]  
    public string? NewEmail { get; set; }
}
