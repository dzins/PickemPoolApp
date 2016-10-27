using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PickemPoolApp.EF
{
    public class Team
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public int TotalPoints
        {
            get
            {
                var totalPoints = 0;

                foreach (var game in this.HomeGames)
                {
                    if (game.HomeTeamScore == game.AwayTeamScore)
                    {
                        totalPoints += 1;
                    }
                    else if (game.HomeTeamScore > game.AwayTeamScore)
                    {
                        totalPoints += 2;
                    }
                }

                foreach (var game in this.AwayGames)
                {
                    if (game.AwayTeamScore == game.HomeTeamScore)
                    {
                        totalPoints += 1;
                    }
                    else if (game.AwayTeamScore > game.HomeTeamScore)
                    {
                        totalPoints += 2;
                    }
                }

                return totalPoints;
            }
        }

        public virtual ICollection<Participant> Participants { get; set; } = new HashSet<Participant>();

        [InverseProperty("HomeTeam")]
        public virtual ICollection<Game> HomeGames { get; set; } = new HashSet<Game>();

        [InverseProperty("AwayTeam")]
        public virtual ICollection<Game> AwayGames { get; set; } = new HashSet<Game>();
    }
}