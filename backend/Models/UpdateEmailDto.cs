using System.ComponentModel.DataAnnotations;

public class UpdateEmailDto
{
    public required string UserId { get; set; }

    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Incorrect email format.")]  
    public required string Email { get; set; }
}