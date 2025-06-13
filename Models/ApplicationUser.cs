using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace VAYTIEN.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(12)]
        public string? CCCD { get; set; }
    }
}
