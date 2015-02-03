using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using CalendarService = Google.Apis.Calendar.v3.CalendarService;
using Settings = CS498.Lib.Properties.Settings;

namespace CS498.Lib
{
    public class MyCalendar
    {
        private readonly ObservableCollection<GoogleEvent> _tasks;
        private static MyCalendar _instance;
        private static CalendarService _service;
        private string _primaryId;
        private List<string> _calendarIds;
        private const string PrimaryId = "PrimaryId";

        private MyCalendar()
        {
            _tasks = new ObservableCollection<GoogleEvent>();
            _primaryId = (string)Settings.Default[PrimaryId];
        }

        public static MyCalendar Instance
        {
            get { return _instance ?? (_instance = new MyCalendar()); }
        }
        public async Task Authorize()
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = PrivateConsts.ClientId,
                    ClientSecret = PrivateConsts.ClientSecrets
                },
                new[] { CalendarService.Scope.Calendar },
                "user",
                CancellationToken.None,
                new FileDataStore("Calendar.Auth.Store"));

            _service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Calendar API Sample"
            });

            await GetAllOwnedCalendars();
            await GetTasks(_primaryId);
        }

        public async Task<ObservableCollection<GoogleEvent>> GetTasks(string id)
        {
            var endTime = DateTime.Now.AddDays((double)TimeBlockChoices.TwoWeeks);
            var lr = _service.Events.List(_primaryId);
            lr.TimeMin = DateTime.Now;
            lr.TimeMax = endTime;
            lr.SingleEvents = true;
            lr.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            var request = await lr.ExecuteAsync();

            foreach (var events in request.Items.Where(events => events.End.DateTime.GetValueOrDefault() > DateTime.Now))
            {
                _tasks.Add(new GoogleEvent
                {
                    Title = events.Summary,
                    Description = events.Description,
                    TimeBlock = new TimeBlock(events.Start.DateTime.GetValueOrDefault(), events.End.DateTime.GetValueOrDefault()),
                    Location = events.Location
                });
            }
            return _tasks;
        }

        public ObservableCollection<TimeBlock> GetFreeTime(int hours, int minutes, DateTime endTime,TimeBlockChoices dateFilter)
        {
            var timeblock = DateTime.Now.AddDays((int) dateFilter);
            var timespan = new TimeSpan(hours, minutes, 0);
            var tasks = _tasks.Where(x => x.TimeBlock.End < timeblock  && x.TimeBlock.End < endTime).ToList();
            var freeTime = new ObservableCollection<TimeBlock>();
            for (var x = 1; x < tasks.Count(); x++)
            {
                if (tasks[x - 1].TimeBlock.End <= tasks[x].TimeBlock.Start && tasks[x - 1].TimeBlock.Start >= DateTime.Now)
                    freeTime.Add(new TimeBlock(tasks[x - 1].TimeBlock.End, tasks[x].TimeBlock.Start));
            }
            if (!tasks.Any())
                return new ObservableCollection<TimeBlock>();
            if (tasks.First().TimeBlock.Start >= DateTime.Now)
                freeTime.Insert(0, new TimeBlock(DateTime.Now, tasks.First().TimeBlock.Start));

            if (tasks.Last().TimeBlock.End < timeblock)
                freeTime.Add(new TimeBlock(tasks.Last().TimeBlock.End, timeblock));
            return new ObservableCollection<TimeBlock>(freeTime.Where(x => x.End - x.Start >= timespan));

        }

        private async Task GetAllOwnedCalendars()
        {
            _calendarIds = new List<string>();
            var req = await _service.CalendarList.List().ExecuteAsync();
            foreach (var calendarListEntry in req.Items.Where(x => x.AccessRole.Equals("owner")))
            {
                _calendarIds.Add(calendarListEntry.Id);
            }
        }

        public async Task<List<string>> GetAllIds()
        {
            if (_calendarIds == null)
                await GetAllOwnedCalendars();
            return _calendarIds;
        }

        public void SetPrimaryId(string id)
        {
            _primaryId = id;
            Settings.Default[PrimaryId] = id;
            Settings.Default.Save();
        }

        public async void AddEvent(GoogleEvent gEvent)
        {
            var calendarEvent = new Event
            {
                Summary = gEvent.Title,
                Description = gEvent.Description,
                Location = gEvent.Location,
                Start = new EventDateTime
                {
                    DateTime = gEvent.TimeBlock.Start,
                },
                End = new EventDateTime
                {
                    DateTime = gEvent.TimeBlock.End,
                }
            };
            await _service.Events.Insert(calendarEvent, _primaryId).ExecuteAsync();

            var googleEvent = _tasks.First(x => x.TimeBlock.Start <= gEvent.TimeBlock.End);
            _tasks.Insert(_tasks.IndexOf(googleEvent), gEvent);
        }

        public string GetIdName()
        {
            return _primaryId;
        }
    }
}
