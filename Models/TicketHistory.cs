using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace TheBugTracker.Models
{
    public class TicketHistory
    {
        public int Id { get; set; }

        [DisplayName("Ticket")]
        public int TicketId { get; set; }

        //which property item of a ticket was modified (indicates)
        [DisplayName("UpdatedItem")]
        public string Property { get; set; }
        //value before modified
        [DisplayName("Previous")]
        public string OldValue { get; set; }
        //value after modified
        [DisplayName("Current")]
        public string NewValue { get; set; }

        [DisplayName("Date Modified")]
        public DateTimeOffset Created { get; set; }

        [DisplayName("Description of Change")]
        public string Description { get; set; }

        //User who updates the History Item.
        [DisplayName("Team Member")]
        public string UserId { get; set; }

        // Navigation Properties
        public virtual Ticket Ticket { get; set; }
        public virtual BTUser User { get; set; }



    }
}
