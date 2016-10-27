using System;
using System.Collections.Generic;
using System.Linq;

namespace PickemPoolApp.EF
{
    public class Participant
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public virtual ICollection<Team> Teams { get; set; } = new HashSet<Team>();

        public int TotalPoints
        {
            get
            {
                return this.Teams.Sum(o => o.TotalPoints);
            }
        }
    }
}