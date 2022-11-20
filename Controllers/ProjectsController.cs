using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBugTracker.Data;
using TheBugTracker.Extensions;
using TheBugTracker.Models;
using TheBugTracker.Models.Enums;
using TheBugTracker.Models.ViewModels;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
       // private readonly ApplicationDbContext _context;
        private readonly IBTRolesService _rolesService;
        private readonly IBTLookupService _lookupService;
        private readonly IBTFileService _fileService;
        private readonly IBTProjectService _projectService;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTCompanyInfoService _companyInfoService;

        public ProjectsController(//ApplicationDbContext context,
                                  IBTRolesService rolesService,
                                  IBTLookupService lookupService,
                                  IBTFileService fileService,
                                  IBTProjectService projectService,
                                  UserManager<BTUser> userManager,
                                  IBTCompanyInfoService companyInfoService)
        {
            //_context = context;
            _rolesService = rolesService;
            _lookupService = lookupService;
            _fileService = fileService;
            _projectService = projectService;
            _userManager = userManager;
            _companyInfoService = companyInfoService;
        }

        public async Task<IActionResult> AddUserToProjectAc(int? id)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            //int companyId = User.Identity.GetCompanyId().Value;
            bool on = await _projectService.AddUserToProjectAsync(_userManager.GetUserId(User), id.Value);

            return RedirectToAction(nameof(MyProjects));
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AssignPM(int id)
        {
            int companyId = User.Identity.GetCompanyId().Value;
            AssignPMViewModel model = new();
            model.Project = await _projectService.GetProjectByIdAsync(id, companyId);
            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(Roles.ProjectManager), companyId), "Id", "FullName");
            return View(model);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> AssignPM(AssignPMViewModel model)
        {
            if (!string.IsNullOrEmpty(model.PMID))
            {
                await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);
                return RedirectToAction(nameof(Details), new { id = model.Project.Id });
            }
            //using the object route values.. (redirectToAction overload #3)
            return RedirectToAction(nameof(AssignPM), new { projectId = model.Project.Id });
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> AssignMembers(int id)
        {
            AddMembersViewModel model = new();
            int companyId = User.Identity.GetCompanyId().Value;
            model.Project = await _projectService.GetProjectByIdAsync(id, companyId);
            model.Users = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(Roles.ProjectManager), companyId), "Id", "FullName");
            List<BTUser> developers = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Developer), companyId);
            List<BTUser> submitters = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Submitter), companyId);
            List<BTUser> companyMembers = developers.Concat(submitters).ToList();

            List<string> projectMembers = model.Project.Members.Select(m => m.Id).ToList();
            model.Users = new MultiSelectList(companyMembers, "Id", "FullName", projectMembers);

            return View(model);


        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [Authorize(Roles = "Admin, ProjectManager")]
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> AssignMembers(AddMembersViewModel model)
        {
            if(model.SelectedUsers != null)
            {
                List<string> memberIds = (await _projectService.GetAllProjectMembersExceptPMAsync(model.Project.Id)).Select(m => m.Id).ToList();

                //remove current members
                foreach(string member in memberIds)
                {
                    await _projectService.RemoveUserFromProjectAsync(member, model.Project.Id);
                }
                //add selected members
                foreach(string mem in model.SelectedUsers)
                {
                    await _projectService.AddUserToProjectAsync(mem, model.Project.Id);
                }
                return RedirectToAction("Details", "Projects", new { id = model.Project.Id });
            }
            return RedirectToAction(nameof(AssignMembers), new { id = model.Project.Id });
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> RemoveUserFromProjectAc(int? id)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            //int companyId = User.Identity.GetCompanyId().Value;
            string userId = _userManager.GetUserId(User);
            BTUser btUser = await _userManager.GetUserAsync(User);

            await _projectService.RemoveUserFromProjectAsync(btUser.Id, id.Value);

            return RedirectToAction(nameof(MyProjects));
        }

        public async Task<IActionResult> MyProjects()
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            int companyId = User.Identity.GetCompanyId().Value;
            string userId = _userManager.GetUserId(User);
            //or
            //var userid = await _userManager.GetUserAsync(User);
            //var idd = userid.UserName;
            var applicationDbContext = await _projectService.GetUserProjectsAsync(userId);
            return View(applicationDbContext);
        }
        public async Task<IActionResult> AllProjects()
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            //need to return different set of data for admin, pm's developers and submitters
            // admin see's all projects, pm all for their company, others - only ones they have been assigned too.
            string userId = _userManager.GetUserId(User);
            int companyId = User.Identity.GetCompanyId().Value;
            try
            {
                if (User.IsInRole(Roles.Admin.ToString()))
                {

                    var model = await _projectService.GetAllProjects();
                    return View(model);
                }
                if (User.IsInRole(Roles.ProjectManager.ToString()))
                {   // perhaps put if block in here for if companyId is null
                    var model = await _companyInfoService.GetAllProjectsAsync(companyId);
                    return View(model);
                }
                else
                {
                    var model = await _projectService.GetUserProjectsAsync(userId);
                    return View(model);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<IActionResult> ArchivedProjects()
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            string userId = _userManager.GetUserId(User);
            int companyId = User.Identity.GetCompanyId().Value;
            try
            {
                if (User.IsInRole(Roles.Admin.ToString()))
                {
                    // there is a GetArchivedProjectsByCompanyAsync methos which could be used as well
                    var model = (await _projectService.GetAllProjects()).Where(p => p.Archived == true);
                    return View(model);
                }
                if (User.IsInRole(Roles.ProjectManager.ToString()))
                {   // perhaps put if block in here for if companyId is null
                    var model = (await _companyInfoService.GetAllProjectsAsync(companyId)).Where(p => p.Archived == true);
                    return View(model);
                }
                else
                {
                    var model = (await _projectService.GetUserProjectsAsync(userId)).Where(p => p.Archived == true);
                    return View(model);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            if (id == null)
            {
                return NotFound();
            }

            //we want the value as it is nullable:
            int companyId = User.Identity.GetCompanyId().Value;

            Project project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // GET: Projects/Create
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Create()
        {
            int companyId = User.Identity.GetCompanyId().Value;
            //add viewMOdel
            AddProjectWithPMViewModel model = new();
            //load up select list with data
            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "id", "Name");

            return View(model);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
        {
            if (model != null)
            {
                int companyId = User.Identity.GetCompanyId().Value;
                model.Project.CompanyId = companyId;
                try
                {
                    if (model.Project.ImageFormFile != null)
                    {
                        model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                        model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                        model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
                    }

                    model.Project.ProjectPriorityId = model.ProjectPriority;

                    await _projectService.AddNewProjectAsync(model.Project);
                    //add pm if one was chosen
                    if (!string.IsNullOrEmpty(model.PmId))
                    {
                        await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
                    }


                }
                catch (Exception)
                {

                    throw;
                }

                //TODO: RedirectToAction to all projects
                return RedirectToAction("Index");
            }
            return RedirectToAction("Create");
        }

        [Authorize(Roles = "Admin, ProjectManager")]
        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            int companyId = User.Identity.GetCompanyId().Value;
            //add viewMOdel
            AddProjectWithPMViewModel model = new();
            model.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            //load up select list with data
            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "id", "Name");

            return View(model);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Edit(AddProjectWithPMViewModel model)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            if (model != null)
            {
                //int companyId = User.Identity.GetCompanyId().Value;
                try
                {
                    if (model.Project.ImageFormFile != null)
                    {
                        model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                        model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                        model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
                    }
                    await _projectService.UpdateProjectAsync(model.Project);
                    //add pm if one was chosen
                    if (!string.IsNullOrEmpty(model.PmId))
                    {
                        await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProjectExistsAsync(model.Project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                //TODO: RedirectToAction to all projects
                return RedirectToAction("Edit");
            }
            return RedirectToAction(nameof(Index));
        }
        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Archive(int? id)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            if (id == null)
            {
                return NotFound();
            }
            int companyId = User.Identity.GetCompanyId().Value;
            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            int companyId = User.Identity.GetCompanyId().Value;
            var project = await _projectService.GetProjectByIdAsync(id, companyId);
            //_context.Projects.Remove(project);
            await _projectService.ArchiveProjectAsync(project);
            //await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Restore(int? id)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            if (id == null)
            {
                return NotFound();
            }
            int companyId = User.Identity.GetCompanyId().Value;
            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Restore")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> RestoreConfirmed(int id)
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        {
            int companyId = User.Identity.GetCompanyId().Value;
            var project = await _projectService.GetProjectByIdAsync(id, companyId);
            await _projectService.RestoreProjectAsync(project);
            //await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ProjectExistsAsync(int id)
        {
            int companyId = User.Identity.GetCompanyId().Value;
            // this is done in fewer lines of code in the ticketclass using the "getall" method the any=>select id.
            var proj = await _projectService.GetProjectByIdAsync(id, companyId);
            if (proj != null)
            {
                return true;
            }
            else return false;
        }
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> UnassignedProjects()
        {
            int companyId = User.Identity.GetCompanyId().Value;
            List<Project> projects = new();

            projects = await _projectService.GetUnassignedProjectAsync(companyId);
            return View(projects);

        }
    }
}
