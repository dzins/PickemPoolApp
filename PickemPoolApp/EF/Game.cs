using System;

namespace PickemPoolApp.EF
{
    public class Game
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string EventId { get; set; }

        public int Week { get; set; }

        public virtual Team HomeTeam { get; set; }

        public virtual Team AwayTeam { get; set; }

        public int HomeTeamScore { get; set; }

        public int AwayTeamScore { get; set; }
    }
}