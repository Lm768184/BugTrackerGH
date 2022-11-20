using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBugTracker.Extensions;
using TheBugTracker.Models;
using TheBugTracker.Models.ViewModels;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Controllers
{
    [Authorize]
    public class UserRolesController : Controller
    {
        private readonly IBTRolesService _rolesService;
        private readonly IBTCompanyInfoService _companyInfoService;
        public UserRolesController(IBTRolesService rolesService, 
                                   IBTCompanyInfoService companyInfoService)
        {
            _rolesService = rolesService;
            _companyInfoService = companyInfoService;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> ManageUserRoles()
        {
            //add an instance of the viewModels as a list(model)
            List<ManageUserRolesViewModel> model = new();

            // Get CompanyId
            int companyId = User.Identity.GetCompanyId().Value;
            

            // Get all company users
            // premise of the app is for people (members) of a company to use/manipulate data within their 
            // own company. So finding the companyId of the logged in User and displaying all the users for that 
            // company is what is done here.
            List<BTUser> users = await _companyInfoService.GetAllMembersAsync(companyId);

            //Loop over the users to populate the ViewModel
            // - Instantiate ViewModel
            // - use _rolesService
            // - Create multi-selects
            foreach(BTUser user in users)
            {
                ManageUserRolesViewModel viewModel = new();
                viewModel.BTUser = user;
                IEnumerable<string> selected = await _rolesService.GetUserRolesAsync(user);
                viewModel.Roles = new MultiSelectList(await _rolesService.GetRolesAsync(),"Name","Name",selected);

                model.Add(viewModel);
            }

            //Return the model to the view
            return View(model);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel member)
        {
            //get the companyId (user has to be logged in for this to work)
            int companyId = User.Identity.GetCompanyId().Value;

            //instantiate the user
            BTUser btUser = (await _companyInfoService.GetAllMembersAsync(companyId)).FirstOrDefault(u => u.Id == member.BTUser.Id);

            //get roles for hte user
            IEnumerable<string> roles = await _rolesService.GetUserRolesAsync(btUser);

            //grab the selected role
            string userRole = member.SelectRoles.FirstOrDefault();

            if(!string.IsNullOrEmpty(userRole))
            {
                //remove user from their roles
                if(await _rolesService.RemoveUserFromRolesAsync(btUser, roles))
                {
                    // add user to the new roles
                    await _rolesService.AddUserToRoleAsync(btUser, userRole);
                }

            }

            //navigate back to the view
            return RedirectToAction(nameof(ManageUserRoles));
        }
    }
}
