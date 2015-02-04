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
        private readonly ObservableCollection<TimeBlock> _freeTime;
        private readonly ObservableCollection<GoogleEvent> _tasks;
        private static MyCalendar _instance;
        private static CalendarService _service;
        private string _primaryId;
        private Dictionary<string, string> _calendarIds;
        private const string PrimaryId = "PrimaryId";

        private MyCalendar()
        {
            _tasks = new ObservableCollection<GoogleEvent>();
            _freeTime = new ObservableCollection<TimeBlock>();
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

            _service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Calendar API Sample"
            });

            if (_primaryId.Equals((string)Settings.Default[PrimaryId]))
                SetPrimaryId(_service.Calendars.Get(_primaryId).Execute().Id);
        }

        public async Task<ObservableCollection<GoogleEvent>> GetTasks(string id)
        {
            _tasks.Clear();
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
            GetFreeTime();
            return _tasks;
        }

        public void GetFreeTime()
        {
            var endTime = DateTime.Now.AddDays((double)TimeBlockChoices.TwoWeeks);
            for (var x = 1; x < _tasks.Count; x++)
            {
                if (_tasks[x - 1].TimeBlock.End <= _tasks[x].TimeBlock.Start)
                    _freeTime.Add(new TimeBlock(_tasks[x - 1].TimeBlock.End, _tasks[x].TimeBlock.Start));
            }
            if (_tasks.First().TimeBlock.Start >= DateTime.Now)
                _freeTime.Insert(0, new TimeBlock(DateTime.Now, _tasks.First().TimeBlock.Start));

            if (_tasks.Last().TimeBlock.End < endTime)
                _freeTime.Add(new TimeBlock(_tasks.Last().TimeBlock.End, endTime));
        }

        public ObservableCollection<TimeBlock> GetFreeTimeBlocks(TimeSpan timeSpan, DateTime endDate, TimeBlockChoices googleDate)
        {
            return new ObservableCollection<TimeBlock>(_freeTime.Where(x => x.Duration < timeSpan && x.End > endDate));
        } 

        private async Task GetAllOwnedCalendars()
        {
            _calendarIds = new Dictionary<string, string>();
            var req = await _service.CalendarList.List().ExecuteAsync();
            foreach (var calendarListEntry in req.Items.Where(x => x.AccessRole.Equals("owner")))
            {
                _calendarIds.Add(calendarListEntry.Id, calendarListEntry.Summary);
            }
        }

        public async Task<Dictionary<string, string>> GetAllIds()
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
            return _calendarIds[_primaryId];
        }
    }
}