using System.ComponentModel.DataAnnotations;

namespace FileStore.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string? LastName { get; set; }

        [Required, DataType(DataType.Password)]
        public string? Password { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
