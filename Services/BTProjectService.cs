using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Models.Enums;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class BTProjectService : IBTProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTRolesService _rolesService;

        public BTProjectService(ApplicationDbContext context,
                                UserManager<BTUser> userManager,
                                IBTRolesService rolesService)
        {
            _context = context;
            _userManager = userManager;
            _rolesService = rolesService;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task AddNewProjectAsync(Project project)
        {
            _context.Add(project);
            await _context.SaveChangesAsync();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<bool> AddProjectManagerAsync(string userId, int projectId)
        {
           
            BTUser currentPM = await GetProjectManagerAsync(projectId);

            if (currentPM != null)
            {
                try
                {
                    await RemoveProjectManagerAsync(projectId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR removeing current PM {ex.Message}");
                    return false;
                }
            }

            
            try
            {
                await AddUserToProjectAsync(userId, projectId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR adding current PM {ex.Message}");
                return false;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<bool> AddUserToProjectAsync(string userId, int projectId)
        {
           
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if (!await IsUserOnProjectAsync(userId, projectId))
                {
                    try
                    {
                        project.Members.Add(user);
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }

            return false;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task ArchiveProjectAsync(Project project)
        {
            try
            {   //how he wrote it:
                project.Archived = true;
                await UpdateProjectAsync(project);

                foreach (Ticket ticket in project.Tickets)
                {
                    ticket.ArchivedByProject = true;
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<Project>> GetAllProjects()
        {
            
            List<Project> projects = await _context.Projects
                                                   .Include(p => p.Company)
                                                   .Include(p => p.Members)
                                                   .Include(p => p.ProjectPriority)
                                                   .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.Notifications)
                                                    .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.History)
                                                    .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.Attachments)
                                                    .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.Comments)
                                                    .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.TicketType)
                                                    .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.TicketPriority)
                                                    .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.TicketStatus)
                                                    .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.OwnerUser)
                                                    .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.DeveloperUser).ToListAsync();
            return projects;


        }

        public async Task<List<BTUser>> GetAllProjectMembersExceptPMAsync(int projectId)
        {
            List<BTUser> developers = await GetProjectMembersByRoleAsync(projectId, Roles.Developer.ToString());
            List<BTUser> sumbitters = await GetProjectMembersByRoleAsync(projectId, Roles.Submitter.ToString());
            List<BTUser> admins = await GetProjectMembersByRoleAsync(projectId, Roles.Admin.ToString());

            List<BTUser> allMembers = developers.Concat(sumbitters).Concat(admins).ToList();

            return allMembers;
            
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<Project>> GetAllProjectsByCompany(int companyId)
        {
            
            List<Project> list = await _context.Projects.Where(p => p.CompanyId == companyId && p.Archived == false)
                                                              .Include(p => p.Members)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.Comments)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.Attachments)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.History)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.Notifications)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.DeveloperUser)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.OwnerUser)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.TicketStatus)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.TicketPriority)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.TicketType)
                                                              .Include(p => p.ProjectPriority)
                                                              .ToListAsync();
            return list;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<Project>> GetAllProjectsByPriority(int companyId, string priorityName)
        {
            List<Project> projects = await GetAllProjectsByCompany(companyId);
            int priId = await LookupProjectPriorityId(priorityName);
            var re = projects.Where(p => p.ProjectPriorityId == priId).ToList();

            return re;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<Project>> GetArchivedProjectsByCompany(int companyId)
        {

            List<Project> list = await _context.Projects.Where(p => p.CompanyId == companyId && p.Archived == true)
                                                              .Include(p => p.Members)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.Comments)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.Attachments)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.History)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.Notifications)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.DeveloperUser)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.OwnerUser)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.TicketStatus)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.TicketPriority)
                                                              .Include(p => p.Tickets)
                                                                 .ThenInclude(t => t.TicketType)
                                                              .Include(p => p.ProjectPriority)
                                                              .ToListAsync();
            return list;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<BTUser>> GetDevelopersOnProjectAsync(int projectId)
        {
            try
            {
                var proj = (await _context.Projects
                                        .Include(p => p.Members)
                                        .FirstOrDefaultAsync(p => p.Id == projectId)).Members.ToList();
                List<BTUser> users = new();

                foreach (BTUser user in proj)
                {
                    if (await _rolesService.IsUserInRoleAsync(user, Roles.Developer.ToString()))
                    {
                        users.Add(user);
                    }

                }
                return users;
            }
            catch (Exception)
            {

                throw;
            }

        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<Project> GetProjectByIdAsync(int projectId, int companyId)
        {
            var res1 = await _context.Projects.Include(p => p.Members)
                                               .Include(p => p.Company)
                                               .Include(p => p.ProjectPriority)
                                               .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.History)
                                                .Include(p => p.Tickets)
                                                    //remember to check if notification nav properties are included
                                                    .ThenInclude(t => t.Notifications)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.Comments)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.Attachments)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.History)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketPriority)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketStatus)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketType)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.DeveloperUser)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.OwnerUser)

                                              .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

            return res1;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<BTUser> GetProjectManagerAsync(int projectId)
        {
            Project project = await _context.Projects
                                            .Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);

            foreach (BTUser user in project?.Members)
            {
                if (await _rolesService.IsUserInRoleAsync(user, Roles.ProjectManager.ToString()))
                {
                    return user;
                }
            }
            return null;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<BTUser>> GetProjectMembersByRoleAsync(int projectId, string role)
        {
            Project project = await _context.Projects.Include(p => p.Members)
                                                     .FirstOrDefaultAsync(p => p.Id == projectId);

            List<BTUser> members = new();

            foreach (var user in project.Members)
            {
                if (await _rolesService.IsUserInRoleAsync(user, role))
                {
                    members.Add(user);
                }

            }

            return members;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<BTUser>> GetSubmittersOnProjectAsync(int projectId)
        {
            {
                try
                {
                    var proj = (await _context.Projects
                                            .Include(p => p.Members)
                                            .FirstOrDefaultAsync(p => p.Id == projectId)).Members.ToList();
                    List<BTUser> users = new();

                    foreach (BTUser user in proj)
                    {
                        if (await _rolesService.IsUserInRoleAsync(user, Roles.Submitter.ToString()))
                        {
                            users.Add(user);
                        }

                    }
                    return users;
                }
                catch (Exception)
                {

                    throw;
                }

            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<Project>> GetUnassignedProjectAsync(int companyId)
        {
            List<Project> result = new();
            List<Project> projects = new();

            try
            {
                projects = await _context.Projects.Include(p => p.ProjectPriority)
                                                  .Where(p => p.CompanyId == companyId)
                                                  .ToListAsync();
                foreach(Project project in projects)
                {
                    if ((await GetProjectMembersByRoleAsync(project.Id, nameof(Roles.ProjectManager))).Count == 0)
                    {
                        result.Add(project);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return result;
        }

        public async Task<List<Project>> GetUserProjectsAsync(string userId)
        {
            try
            {
                var userProjects = (await _context.Users
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Company)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Members)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Tickets)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Tickets)
                                                   .ThenInclude(t => t.DeveloperUser)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Tickets)
                                                    .ThenInclude(t => t.OwnerUser)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketPriority)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketStatus)
                                            .Include(u => u.Projects)
                                                .ThenInclude(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketType)
                                            .FirstOrDefaultAsync(u => u.Id == userId)).Projects.ToList();
                return userProjects;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"**** ERROR ***** Erorr getting user projects list --> {ex.Message}");
                throw;

            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<BTUser>> GetUsersNotOnProjectAsync(int projectId, int companyId)
        {
            List<BTUser> users = await _context.Users.Where(u => u.Projects.All(p => p.Id != projectId)).ToListAsync();
            return users.Where(u => u.CompanyId == companyId).ToList();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<bool> IsUserAssignedAsProjectManager(string userId, int projectId)
        {
            try
            {
                string projectManagerId = (await GetProjectManagerAsync(projectId))?.Id;
                if (projectManagerId == userId)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<bool> IsUserOnProjectAsync(string userId, int projectId)
        {
            var res = (await _context.Projects.Include(p => p.Members)
                                             .FirstOrDefaultAsync(p => p.Id == projectId))
                                             .Members.Select(m=>m.Id).ToList();
            
            bool isOn = res.Contains(userId);
            return isOn;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<int> LookupProjectPriorityId(string priorityName)
        {
            var res = (await _context.ProjectPriorities.FirstOrDefaultAsync(p => p.Name == priorityName)).id;
            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task RemoveProjectManagerAsync(int projectId)
        {
            Project project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);

            try
            {
                foreach (var user in project?.Members)
                {
                    if (await _rolesService.IsUserInRoleAsync(user, Roles.ProjectManager.ToString()))
                    {
                        await RemoveUserFromProjectAsync(user.Id, project.Id);
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task RemoveUserFromProjectAsync(string userId, int projectId)
        {
            try
            {
                BTUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

                try
                {
                    if (await IsUserOnProjectAsync(userId, projectId))
                    {
                        project.Members.Remove(user);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"**** ERROR **** - Error Removing User from Project. ---> {ex.Message}");
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task RemoveUsersFromProjectByRoleAsync(string role, int projectId)
        {
            try
            {
                List<BTUser> members = await GetProjectMembersByRoleAsync(projectId, role);
                Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                try
                {
                    foreach (var user in members)
                    {
                        project.Members.Remove(user);
                    }
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"**** ERROR **** occured whilst attempting to remove users from project -the role could not be found, or there are no users in that role to remove form the project");
            }

        }

        public async Task RestoreProjectAsync(Project project)
        {
            try
            {   
                project.Archived = false;
                await UpdateProjectAsync(project);

                foreach (Ticket ticket in project.Tickets)
                {
                    ticket.ArchivedByProject = false;
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task UpdateProjectAsync(Project project)
        {
            _context.Update(project);
            await _context.SaveChangesAsync();
        }
    }
}
