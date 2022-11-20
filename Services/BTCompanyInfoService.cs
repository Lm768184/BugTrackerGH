using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class BTCompanyInfoService : IBTCompanyInfoService
    {
        private readonly ApplicationDbContext _context;

        public BTCompanyInfoService(ApplicationDbContext context)
        {
            _context = context;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<BTUser>> GetAllMembersAsync(int companyId)
        {
            List<BTUser> list = await _context.Users.Where(u => u.CompanyId == companyId).ToListAsync();
            return list;
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<List<Project>> GetAllProjectsAsync(int companyId)
        {

            List<Project> list = await _context.Projects.Where(p => p.CompanyId == companyId)
                                                              .Include(p=>p.Members) 
                                                              .Include(p=>p.Tickets)
                                                                 .ThenInclude(t =>t.Comments)
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
        public async Task<List<Ticket>> GetAllTicketsAsync(int companyId)
        {
            try
            {
                var ticks = (await _context.Projects.Include(p => p.Tickets)
                                               .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketPriority)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketStatus)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketType)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.TicketPriority)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.Comments)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.Attachments)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.Notifications)
                                                .Include(p => p.Tickets)
                                                    .ThenInclude(t => t.History)
                                                .FirstOrDefaultAsync(p => p.CompanyId.Value == companyId)).Tickets.ToList();
                return ticks;
            }
            catch (Exception)
            {

                throw;
            }
            
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async Task<Company> GetCompanyInfoByIdAsync(int? companyId)
        {
            if (companyId is not null)
            { 
                var ci = await _context.Companies
                                        .Include(c=>c.Members)
                                        .Include(c=>c.Projects)
                                        .Include(c=>c.Invites)
                                        .FirstOrDefaultAsync(c => c.Id == companyId);
                return ci;
            }
            else 
            {
                var ci = await _context.Companies.FirstOrDefaultAsync(c => c.Id == 0);
                return ci;
            }
        }
    }
}
