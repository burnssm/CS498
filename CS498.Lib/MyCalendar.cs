using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using CalendarService = Google.Apis.Calendar.v3.CalendarService;

namespace CS498.Lib
{
    public class MyCalendar
    {
        private static MyCalendar _instance;
        private static CalendarService _service;
        private Dictionary<string, string> _calendarIds;
        private string _primaryId = "primary";

        private MyCalendar()
        {
        }

        public static MyCalendar Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyCalendar();;
                return _instance;
            }
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
        }

        private void GetAllOwnedCalendars()
        {
            _calendarIds = new Dictionary<string, string>();
            var req = _service.CalendarList.List().Execute();
            foreach (var calendarListEntry in req.Items.Where(x => x.AccessRole.Equals("owner")))
            {
                _calendarIds.Add(calendarListEntry.Summary, calendarListEntry.Id);
            }
        }

        public Dictionary<string, string> GetAllIds()
        {
            if (_calendarIds == null)
                GetAllOwnedCalendars();
            return _calendarIds;
        }

        public void SetPrimaryId(string id)
        {
            _primaryId = id;
        }

        public void AddEvent(string title, string description, string location, DateTime startDateTime, DateTime endDateTime)
        {
            var calendarEvent = new Event
            {
                Summary = title,
                Description = description,
                Location = location,
                Start = new EventDateTime
                {
                    DateTime = startDateTime,
                },
                End = new EventDateTime
                {
                    DateTime = endDateTime,
                }
            };

            _service.Events.Insert(calendarEvent, _primaryId).Execute();
            
        }

        public async void GetFreeTime()
        {
            var calendar = _service.Calendars.Get(_primaryId).Execute();
            EventsResource.ListRequest lr = _service.Events.List(entry.Id);
            lr.TimeMin = DateTime.Now;
            lr.TimeMax = DateTime.Now.AddDays(7);
            lr.SingleEvents = true;
            lr.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            var request = await lr.ExecuteAsync();
            var events = _service.Events.List(_primaryId).Execute();
            var y = new EventDateTime()
            {                    
                DateTime =  DateTime.Now 
            };
            foreach (var es in events.Items) //.Where(x => x.Start.DateTime > y.DateTime))
            {
                Console.Out.WriteLine(es.Summary);
                Console.Out.WriteLine(es.Start.DateTimeRaw);
                Console.Out.WriteLine(es.End.DateTimeRaw);
            }
        }
    }
}
