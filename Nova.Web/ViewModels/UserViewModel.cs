using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Nova.Web.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }

        [DisplayName("First Name")]
        [Required(ErrorMessage = "First Name is required")]
        public string Firstname { get; set; }

        [DisplayName("Last Name")]
        [Required(ErrorMessage = "Last Name is required")]
        public string Lastname { get; set; }

        public string? Fullname { get; set; }


        [DisplayName("User Name")]
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [DisplayName("Password")]
        [Required(ErrorMessage = "Password is required")]
        [StringLength(25, MinimumLength = 5)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address")]
        [MaxLength(50)]
        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-za-z]{2,4}", ErrorMessage = "Please enter correct email")]
        public string Email { get; set; }

        [DisplayName("New Password")]
        [Required(ErrorMessage = "New Password is required")]
        [StringLength(25, ErrorMessage = "Must be between 5 and 25 characters", MinimumLength = 5)]
        [DataType(DataType.Password)]
        public String NewPassword { get; set; }

        [DisplayName("Confirm Password")]
        [StringLength(25, ErrorMessage = "Must be between 5 and 25 characters", MinimumLength = 5)]
        [DataType(DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public int RoleId { get; set; }
        public string? Rolename { get; set; }

        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
        public int UserId { get; set; }

        public bool RememberMe { get; set; } = false;

        public string? TwoFactorCode { get; set; }
        public DateTime? TwoFactorCodeExpiry { get; set; }

        public List<SelectListItem>? ddlRoles { get; set; }
        public string? Phone { get; set; }
        public bool? isPassword { get; set; } = true;
    }
}
