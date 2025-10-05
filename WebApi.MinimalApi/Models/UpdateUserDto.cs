using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UpdateUserDto
{
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    public string Login { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "First name cannot be empty.")]
    public string FirstName { get; set; }
    
    [Required(AllowEmptyStrings = false, ErrorMessage = "Last name cannot be empty.")]
    public string LastName { get; set; }
}