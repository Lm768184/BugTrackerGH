using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheBugTracker.Models.ViewModels
{
    public class AddMembersViewModel
    {
        public Project Project { get; set; }
        //the select list needed to supply the the list of members to the user:
        public MultiSelectList Users { get; set; }
        // need another list to store the users selections to send back to the post method:
        public List<string> SelectedUsers { get; set; }
    }
}
