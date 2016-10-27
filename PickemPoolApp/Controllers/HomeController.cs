using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using Excel;
using Newtonsoft.Json;
using PickemPoolApp.EF;

namespace PickemPoolApp.Controllers
{
    public class HomeController : Controller
    {
        public int CurrentWeek
        {
            get
            {
                var currentWeek = DateTimeFormatInfo.CurrentInfo.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday) - 36;
                if (currentWeek > 17)
                {
                    currentWeek = 17;
                }

                return currentWeek;
            }
        }

        //[Authorize]
        public ActionResult Index()
        {
            using (var context = new PickemPoolContext())
            {
                return View(this.GetParticipants(context));
            }
        }

        private IList<Participant> GetParticipants(PickemPoolContext context)
        {
            // if we already read the spreadsheet, return the data from the db
            if (context.Participants.Any())
            {
                this.ReadEspn(context, context.Teams.Include(o => o.HomeGames).Include(o => o.AwayGames).ToList());
                return context.Participants.Include(o => o.Teams).ToList();
            }

            var file = Server.MapPath(@"~/App_Data/pickem.xlsx");

            var stream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read);

            IExcelDataReader excelReader = null;

            if (file.EndsWith(".xlsx"))
            {
                excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            }
            else if (file.EndsWith(".xls"))
            {
                excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(file), "Only .xlsx, .xls and .csv files are supported");
            }

            excelReader.IsFirstRowAsColumnNames = true;

            var dataSet =  excelReader.AsDataSet();

            var dataTable = dataSet.Tables[0];

            var teams = new List<Team>();

            for (var i = 1; i <= 32; i++)
            {
                teams.Add(new Team
                {
                    Name = dataTable.Columns[i].ColumnName
                });
            }

            var participants = new List<Participant>();

            foreach (DataRow row in dataTable.Rows)
            {
                if (string.IsNullOrWhiteSpace(row[0].ToString()))
                {
                    break;
                }

                var participant = new Participant
                {
                    Name = row[0].ToString()
                };

                for (var i = 1; i <= 32; i++)
                {
                    if (!string.IsNullOrWhiteSpace(row[i].ToString()))
                    {
                        participant.Teams.Add(teams[i - 1]);
                    }
                }

                participants.Add(participant);
            }

            context.Teams.AddRange(teams);
            context.SaveChanges();

            context.Participants.AddRange(participants);
            context.SaveChanges();

            this.ReadEspn(context, teams);

            return participants;
        }

        private void ReadEspn(PickemPoolContext context, IList<Team> teams)
        {

            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 0, 0, 30);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                for (var weekNumber = 1; weekNumber <= this.CurrentWeek; weekNumber++)
                {
                    dynamic espnData = HttpContext.Cache.Get($"EspnData{weekNumber}");
                    if (espnData != null)
                    {
                        ParseEspnData(context, teams, weekNumber, espnData);
                        continue;
                    }

                    var espnUrl = $"http://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard?lang=en&region=us&calendartype=blacklist&limit=100&dates=2016&seasontype=2&week={weekNumber}";
                    var response = client.GetAsync(espnUrl).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        espnData = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                        if (!ParseEspnData(context, teams, weekNumber, espnData))
                        {
                            break;
                        }

                        HttpContext.Cache.Add($"EspnData{weekNumber}", espnData, null, DateTime.Now.AddMinutes(30), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
                    }
                    else
                    {
                        var message = $"There was a problem connecting to the endpoint using address: {espnUrl}. {response.ReasonPhrase}";
                        throw new DataException(message);
                    }
                }
            }

            context.SaveChanges();
        }

        private bool ParseEspnData(PickemPoolContext context, IList<Team> teams, int week, dynamic espnData)
        {
            try
            {
                foreach (var espnEvent in espnData.events)
                {
                    if (!espnEvent.status.type.completed.Value)
                    {
                        continue;
                    }

                    string eventId = espnEvent.id.Value.ToString();

                    if (context.Games.Any(o => o.EventId == eventId && o.Week == week))
                    {
                        continue;
                    }

                    foreach (var competition in espnEvent.competitions)
                    {
                        var game = new Game { EventId = eventId, Week = week };
                        foreach (var competitor in competition.competitors)
                        {
                            var team = GetTeam(teams, competitor);
                            if (competitor.homeAway.Value.ToString().Equals("home", StringComparison.OrdinalIgnoreCase))
                            {
                                game.HomeTeam = team;
                                game.HomeTeamScore = Convert.ToInt32(competitor.score.Value);
                            }
                            else
                            {
                                game.AwayTeam = team;
                                game.AwayTeamScore = Convert.ToInt32(competitor.score.Value);
                            }
                        }

                        context.Games.Add(game);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Team GetTeam(IList<Team> teams, dynamic competitor)
        {
            var location = competitor.team.location.Value.ToString();
            var name = competitor.team.name.Value.ToString();
            if (name == "Giants")
            {
                location = "NY Giants";
            }
            else if (name == "Jets")
            {
                location = "NY Jets";
            }

            var team = teams.FirstOrDefault(o => o.Name.Equals(location, StringComparison.OrdinalIgnoreCase));
            if (team == null)
            {
                throw new HttpException($"Unable to find team for {location}");
            }

            return team;
        }
    }
}