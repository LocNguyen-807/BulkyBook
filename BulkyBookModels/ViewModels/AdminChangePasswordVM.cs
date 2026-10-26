using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BulkyBookModels.ViewModels
{
    public class AdminChangePasswordVM
    {
        public string UserId { get; set; }

        [Display(Name = "User Email")]
        public string UserEmail { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage ="The new password and confirm password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
