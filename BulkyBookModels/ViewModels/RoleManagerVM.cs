using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBookModels.ViewModels
{
    public class RoleManagerVM
    {
        public ApplicationUser? ApplicationUser { get; set; }
        public IEnumerable<SelectListItem> RoleList { get; set; }
        public string CurrentRole { get; set; } = string.Empty;
        public string SelectedRole { get; set; } = string.Empty;
    }
}
