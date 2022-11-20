using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace TheBugTracker.Models
{
    public class ProjectPriority
    {
        //pk
        public int id { get; set; }

        [DisplayName("Priority Name")]
        public string Name { get; set; }
    }
}
