using System.Data.Entity;
using PickemPoolApp.Models;

namespace PickemPoolApp.EF
{
    public class PickemPoolContext : DbContext
    {
        public PickemPoolContext() : base("name=DefaultConnection")
        {
        }

        public static void ConfigureDatabase()
        {
            /*
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<PickemPoolContext, Configuration>());
            using (var context = new PickemPoolContext())
            {
                context.Database.Initialize(true);
            }
            */
        }

        public virtual DbSet<Participant> Participants { get; set; }

        public virtual DbSet<Team> Teams { get; set; }

        public virtual DbSet<Game> Games { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        /*
            modelBuilder.Entity<Participant>()
                .HasMany(o => o.Teams)
                .WithMany(o => o.Participants);
                */

            base.OnModelCreating(modelBuilder);
        }
    }
}