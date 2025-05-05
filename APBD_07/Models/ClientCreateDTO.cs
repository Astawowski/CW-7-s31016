using System.ComponentModel.DataAnnotations;

namespace APBD_07.Models;

public class ClientCreateDTO
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string FirstName { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string LastName { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 1)]
    [RegularExpression(@"^.+\@.+\..+$",ErrorMessage = "Incorrect email address.")]
    public string Email { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 1)]
    [RegularExpression(@"^\+\d{11}$", ErrorMessage = "Incorrect phone number.")]
    public string Telephone { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 1)]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Incorrect PESEL")]
    public string Pesel { get; set; }
}
