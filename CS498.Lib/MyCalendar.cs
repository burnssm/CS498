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

        private MyCalendar() {}

        public static MyCalendar Instance
        {
            get { return _instance ?? (_instance = new MyCalendar()); }
        }

        private static async Task<CalendarService> GetService()
        {
            if (_service != null) return _service;
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
            return _service;
        }

        public async Task<IList<Event>> GetAllEventsAsync(string id,int numberOfDays)
        {
            var endTime = DateTime.Now.AddDays(numberOfDays);
            var service = await GetService();
            var lr = service.Events.List(id);
            lr.TimeMin = DateTime.Now;
            lr.TimeMax = endTime;
            lr.SingleEvents = true;
            lr.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            var request = await lr.ExecuteAsync();
            return request.Items;
        }

        public async Task AddEventToCalendarAsync(string id, Event calendarEvent)
        {
            var service = await GetService();
            await service.Events.Insert(calendarEvent, id).ExecuteAsync();
        }

        /// <summary>
        /// THIS IS NOT FINISHED - this does not adjust task list or free time list
        /// as it should - had it been completely implemented
        /// Don't grade us on this 
        /// </summary>
        public async Task DeleteEventFromCalendarAsync(string id, GoogleEvent deletedEvent)
        {
            var service = await GetService();
            await service.Events.Delete(id, deletedEvent.Id).ExecuteAsync();
        }

        public async Task<Dictionary<string, string>> GetAllCalendarsIdsAsync()
        {
            var service = await GetService();
            var calendarIds = new Dictionary<string, string>();
            var req = await service.CalendarList.List().ExecuteAsync();
            foreach (var calendarListEntry in req.Items.Where(x => x.AccessRole.Equals("owner")))
            {
                var calendarId = calendarListEntry.Id;
                var calendarSummary = calendarListEntry.Summary;
                if (calendarIds.ContainsKey(calendarListEntry.Id))
                {
                    calendarId += calendarSummary;
                }
                calendarIds.Add(calendarId, calendarSummary);
            }
            return calendarIds;
        }
    }
}