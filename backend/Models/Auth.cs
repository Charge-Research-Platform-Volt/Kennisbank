using System.ComponentModel.DataAnnotations;

namespace KnowledgeBank.Models;
public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class ForgotPasswordDto
{
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Incorrect email format.")]
    public required string Email { get; set; }
}

public class ResetPasswordDto
{
    public required string Email { get; set; }
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
}