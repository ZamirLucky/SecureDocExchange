using System.ComponentModel.DataAnnotations;

namespace FileStore.ViewModels
{
    public class DownloadViewModel
    {
        [Required, EmailAddress]
        public string? LawyerEmail { get; set; }

        [Required]
        [RegularExpression("^[A-Fa-f0-9]{32}$",
            ErrorMessage = "Code must be 32 hex characters.")]
        public string? Code { get; set; }
    }
}
