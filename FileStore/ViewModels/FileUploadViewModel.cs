using System.ComponentModel.DataAnnotations;

namespace FileStore.ViewModels
{
    public class FileUploadViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Client Email")]
        public string? ClientEmail { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Lawyer Email")]
        public string? LawyerEmail { get; set; }

        [Required]
        [DataType(DataType.Upload)]
        [Display(Name = "Select DOCX File")]
        public IFormFile? File { get; set; }
    }
}
