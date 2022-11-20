using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
    public class TicketsController : Controller
    {
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTProjectService _projectService;
        private readonly IBTTicketService _ticketService;
        private readonly IBTLookupService _lookupService;
        private readonly IBTFileService _fileService;
        private readonly IBTTicketHistoryService _ticketHistoryService;
        private readonly IBTRolesService _rolesService;

        public TicketsController(
                                UserManager<BTUser> userManager,
                                IBTProjectService projectService,
                                IBTTicketService ticketService,
                                IBTLookupService lookupService,
                                IBTFileService fileService,
                                IBTTicketHistoryService ticketHistoryService, 
                                IBTRolesService rolesService)
        {
            
            _userManager = userManager;
            _projectService = projectService;
            _ticketService = ticketService;
            _lookupService = lookupService;
            _fileService = fileService;
            _ticketHistoryService = ticketHistoryService;
            _rolesService = rolesService;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
        {
            string statusMessage;

            if (ModelState.IsValid && ticketAttachment.FormFile != null)
            {
                ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
                ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
                ticketAttachment.FileContentType = ticketAttachment.FormFile.ContentType;

                ticketAttachment.Created = DateTimeOffset.Now;
                ticketAttachment.UserId = _userManager.GetUserId(User);

                await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
                statusMessage = "Success: New attachment added to Ticket.";

                //add history
                await _ticketHistoryService.AddHistoryAsync(ticketAttachment.TicketId, nameof(TicketAttachment), ticketAttachment.UserId);
            }
            else
            {
                statusMessage = "Error: Invalid data.";

            }

            return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketComment([Bind("Id,TicketId,Comment")] TicketComment ticketComment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    ticketComment.UserId = _userManager.GetUserId(User);
                    ticketComment.Created = DateTimeOffset.Now;

                    await _ticketService.AddTicketCommentAsync(ticketComment);
                    //add history:
                    await _ticketHistoryService.AddHistoryAsync(ticketComment.TicketId, nameof(TicketComment), ticketComment.UserId);
                }
                catch (Exception)
                {

                    throw;
                }
            }
            return RedirectToAction("Details", new { id = ticketComment.TicketId });
        }
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        // POST: Tickets/Delete/5
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
            ticket.Archived = true;
            await _ticketService.UpdateTicketAsync(ticket);
            return RedirectToAction(nameof(Index));
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> AllTickets()
        {
            BTUser btUser = await _userManager.GetUserAsync(User);
            int companyId = User.Identity.GetCompanyId().Value;

            List<Ticket> tickets = await _ticketService.GetAllTicketsByCompanyAsync(btUser.CompanyId);

            if(User.IsInRole(nameof(Roles.DemoUser))  || User.IsInRole(nameof(Roles.Submitter)))
            {
                return View(tickets.Where(t => t.Archived == false));
            }
            else
            {
                return View(tickets);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> ArchivedTickets()
        {
            BTUser btUser = await _userManager.GetUserAsync(User);
            int companyId = User.Identity.GetCompanyId().Value;
            try
            {
                if (User.IsInRole(nameof(Roles.ProjectManager)))
                {
                    List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(btUser.CompanyId);
                    return View(tickets);
                }
                if (User.IsInRole(nameof(Roles.Admin)))
                {
                    List<Ticket> tickets = (await _ticketService.GetAllTicketsForAdmin()).Where(t => t.Archived).ToList();
                    return View(tickets);
                }
                else
                {
                    List<Ticket> tickets = (await _ticketService.GetTicketsByUserIdAsync(btUser.Id, companyId)).Where(t => t.Archived).ToList();
                    return View(tickets);
                }
            }
            catch (Exception)
            {

                throw;
            }
            
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // GET: Tickets/Details/5
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignDeveloper(int id)
        
        {
            AssignDeveloperViewModel model = new();
            model.Ticket = await _ticketService.GetTicketByIdAsync(id);
            model.Developers = new SelectList(await _projectService.GetProjectMembersByRoleAsync(model.Ticket.ProjectId, Roles.Developer.ToString()), "Id", "FullName");

            return View(model);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel model)
        
        {
            if(model.DeveloperId !=null)
            { // assign the ticket to the developer
                BTUser btUser = await _userManager.GetUserAsync(User);
                Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);
                try
                {
                    await _ticketService.AssignTicketAsync(model.Ticket.Id, model.DeveloperId);

                }
                catch (Exception)
                {

                    throw;
                }
                //newTicket
                Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);
                await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);
                return RedirectToAction(nameof(Details), new { id = model.Ticket.Id });
            }
            return RedirectToAction(nameof(AssignDeveloper), new { id = model.Ticket.Id });
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // GET: Tickets/Create
        public async Task <IActionResult> Create()
        {
            BTUser btUser = await _userManager.GetUserAsync(User);
            int companyId = User.Identity.GetCompanyId().Value;
            List<Ticket> tickets = new();

            if(User.IsInRole(nameof(Roles.Admin)))
            { 
                ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjects(), "Id", "Name");
            }
            else if (User.IsInRole(nameof(Roles.ProjectManager)))
            { 
                ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompany(companyId), "Id", "Name"); 
            }
            else
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(btUser.Id), "Id", "Name");
            }

            ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
            ViewData["DeveloperUser"] = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.Developer.ToString(),companyId), "Id", "FullName");


            return View();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,DeveloperUserId,OwnerUserId")] Ticket ticket)
        {
            BTUser btUser = await _userManager.GetUserAsync(User);
            int companyId = User.Identity.GetCompanyId().Value;

            if (ModelState.IsValid)
            {
                try
                {
                    ticket.Created = DateTime.Now;
                    ticket.TicketStatusId = 4;
                    ticket.OwnerUserId = _userManager.GetUserId(User);
                    await _ticketService.AddNewTicketAsync(ticket);

                    Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
                    await _ticketHistoryService.AddHistoryAsync(null, newTicket, btUser.Id);
                }
                catch (Exception)
                {

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            // if model state is invalid, return index view with appropriate viewbags
            if (User.IsInRole(nameof(Roles.Admin)))
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjects(), "Id", "Name");
            }
            else if (User.IsInRole(nameof(Roles.ProjectManager)))
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompany(companyId), "Id", "Name");
            }
            else
            {
                ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(btUser.Id), "Id", "Name");
            }

            ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
            ViewData["DeveloperUser"] = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.Developer.ToString(), companyId), "Id", "FullName");


            return View();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var companyId = User.Identity.GetCompanyId().Value;
            if (id == null)
            {
                return NotFound();
            }

            //var ticket = await _context.Tickets.FindAsync(id);
            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

            if (ticket == null)
            {
                return NotFound();
            }
            
            ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompany(companyId));
            ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name",ticket.TicketTypeId);
            ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusAsync(), "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name",ticket.TicketPriorityId);


            return View(ticket);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // POST: Tickets/Edit/5
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,DeveloperUserId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                BTUser btuser = await _userManager.GetUserAsync(User);
                Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);

                try
                {
                    ticket.Updated = DateTimeOffset.Now;
                    await _ticketService.UpdateTicketAsync(ticket);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await TicketExistsAsync(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                //TODO: Add Ticket History
                Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
                await _ticketHistoryService.AddHistoryAsync(oldTicket, newTicket, btuser.Id);

                return RedirectToAction(nameof(Index));
            }
            
            ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);
            ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusAsync(), "Id", "Name");
            ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
            return View(ticket);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // GET: Tickets/Delete/5
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Restore(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------
        
        //[ValidateAntiForgeryToken] 
        public async Task<IActionResult> MyTickets()
        {
            BTUser btUser = await _userManager.GetUserAsync(User);

            List<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(btUser.Id, btUser.CompanyId);
            return View(tickets);
        }
        //----------------------------------------------------------------------------------------------------------------------------------

        // GET: Tickets
        [HttpPost, ActionName("Restore")]
        public async Task<IActionResult> RestoreConfirmed(int? id)
        {
            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);
            ticket.Archived = false;
            await _ticketService.UpdateTicketAsync(ticket);
            return RedirectToAction(nameof(Index));
        }
        //---------------------------------------------------------------------------------------------------------------------------------
        [Authorize(Roles="Admin, ProjectManager")]

        public async Task<IActionResult> UnassignedTickets()
        { // tickets are classed as "unassigned" when they have not been assigned a developer
            int companyId = User.Identity.GetCompanyId().Value;
            string btuserId = _userManager.GetUserId(User);

                    List<Ticket> tickets = await _ticketService.GetUnassignedTicketsAsync(companyId);

            if(User.IsInRole(nameof(Roles.Admin)))
            {
                return View(tickets);
            }
            else
            {
                List<Ticket> pmTickets = new();
                foreach(Ticket ticket in tickets)
                {
                    if(await _projectService.IsUserAssignedAsProjectManager(btuserId, ticket.ProjectId))
                    {
                        pmTickets.Add(ticket);
                    }
                }
            }
                    return View(tickets);
        }
        //---------------------------------------------------------------------------------------------------------------------------------
       
        public async Task<IActionResult> ShowFile(int id)
        {
            TicketAttachment ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(id);
            string fileName = ticketAttachment.FileName;
            byte[] fileData = ticketAttachment.FileData;
            string ext = Path.GetExtension(fileName).Replace(".", "");

            Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
            return File(fileData, $"application/{ext}");
        }
        private async Task<bool> TicketExistsAsync(int id)
        {
            int companyId = User.Identity.GetCompanyId().Value;
            return (await _ticketService.GetAllTicketsByCompanyAsync(companyId)).Any(t => t.Id == id);
        }
    }
}
