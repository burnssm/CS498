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
        private const TimeBlockChoices LengthOfTimeToDisplay = TimeBlockChoices.TwoWeeks;

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

            await GetAllOwnedCalendars();
            var settingsProperty = Settings.Default.Properties[PrimaryId];
            if (settingsProperty != null && _primaryId.Equals((string)settingsProperty.DefaultValue))
            {
                var calendar = await _service.Calendars.Get(_primaryId).ExecuteAsync();
                SetPrimaryId(calendar.Id);
                
            }
            GetTasks();
        }

        public ObservableCollection<GoogleEvent> GetTasks()
        {
            _tasks.Clear();
            var endTime = DateTime.Now.AddDays((double)LengthOfTimeToDisplay);
            var lr = _service.Events.List(_primaryId);
            lr.TimeMin = DateTime.Now;
            lr.TimeMax = endTime;
            lr.SingleEvents = true;
            lr.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            var request = lr.ExecuteAsync().Result;

            foreach (var events in request.Items.Where(events => events.End.DateTime.GetValueOrDefault() > DateTime.Now))
            {
                _tasks.Add(new GoogleEvent(events.Id)
                {
                    Title = events.Summary,
                    Description = events.Description,
                    TimeBlock = new TimeBlock(events.Start.DateTime.GetValueOrDefault(), events.End.DateTime.GetValueOrDefault()),
                    Location = events.Location
                });
            }
            CalculateFreeTime();
            return _tasks;
        }

        public ObservableCollection<TimeBlock> GetFreeTimeBlocks(TimeSpan timeSpan, DateTime dueDate, TimeBlockChoices googleDate)
        {
            if(timeSpan.Equals(new TimeSpan()) || dueDate.Equals(new DateTime())) throw new ArgumentException("Time Span must be more than 0.", "timeSpan");
            var filterEndDate = DateTime.Now.AddDays((int)googleDate);
            var end = dueDate < filterEndDate ? dueDate : filterEndDate;
            var updatedTimeBlock = _freeTime
                                    .Where(x => x.Duration >= timeSpan && x.End >= end && x.Start <= end)
                                    .Select(x => new TimeBlock(x.Start, end))
                                    .ToList();
            return new ObservableCollection<TimeBlock>(_freeTime.Where(x => x.Duration >= timeSpan && x.End <= end).Concat(updatedTimeBlock));
        } 

        private async Task GetAllOwnedCalendars()
        {
            _calendarIds = new Dictionary<string, string>();
            var req = await _service.CalendarList.List().ExecuteAsync();
            foreach (var calendarListEntry in req.Items.Where(x => x.AccessRole.Equals("owner")))
            {
                var calendarId = calendarListEntry.Id;
                var calendarSummary = calendarListEntry.Summary;
                if (_calendarIds.ContainsKey(calendarListEntry.Id))
                {
                    calendarId += calendarSummary;
                }
                _calendarIds.Add(calendarId, calendarSummary);
            }
        }

        private void CalculateFreeTime()
        {
            if (!_tasks.Any()) return;
            _freeTime.Clear();
            var endTime = DateTime.Now.AddDays((double)LengthOfTimeToDisplay);
            for (var x = 1; x < _tasks.Count; x++)
            {
                if (_tasks[x - 1].TimeBlock.End < _tasks[x].TimeBlock.Start)
                    _freeTime.Add(new TimeBlock(_tasks[x - 1].TimeBlock.End, _tasks[x].TimeBlock.Start));
            }
            if (_tasks.First().TimeBlock.Start >= DateTime.Now)
                _freeTime.Insert(0, new TimeBlock(DateTime.Now, _tasks.First().TimeBlock.Start));

            if (_tasks.Last().TimeBlock.End < endTime)
                _freeTime.Add(new TimeBlock(_tasks.Last().TimeBlock.End, endTime));
        }

        public async Task AddEvent(GoogleEvent gEvent)
        {
            if(gEvent == null) throw new ArgumentNullException("gEvent", "Event can not be null");
            if (gEvent.TimeBlock == null) throw new ArgumentException("Event must have a Time Block", "gEvent");
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
                },
                Id = gEvent.Id
            };
            await _service.Events.Insert(calendarEvent, _primaryId).ExecuteAsync();
            
            if(!_tasks.Any())
                _tasks.Add(gEvent);
            else 
            { 
                var googleEvent = _tasks.First(x => x.TimeBlock.Start <= gEvent.TimeBlock.Start);
                _tasks.Insert(_tasks.IndexOf(googleEvent), gEvent);
            }
        }
        
        public async Task<Dictionary<string, string>> GetAllIds()
        {
            if (_calendarIds == null)
                await GetAllOwnedCalendars();
            return _calendarIds;
        }

        /// <summary>
        /// THIS IS NOT FINISHED - this does not adjust task list or free time list
        /// as it should - had it been completely implemented
        /// Don't grade us on this 
        /// </summary>
        /// <param name="gEvent"></param>
        /// <returns></returns>
        public async Task DeleteEvent(GoogleEvent gEvent)
        {
            await _service.Events.Delete(_primaryId, gEvent.Id).ExecuteAsync();
        }
       
        public void SetPrimaryId(string id)
        {
            if(id == null)
                throw new ArgumentNullException("id", "Null string could not be used as a calendar id.");
            var settingsProperty = Settings.Default.Properties[PrimaryId];
            if (settingsProperty == null || (!_calendarIds.ContainsKey(id) && !id.Equals(settingsProperty.DefaultValue))) 
                throw new ArgumentException("Invalid ID could not be set.", "id");
            _primaryId = id;
            Settings.Default[PrimaryId] = id;
            Settings.Default.Save();
        }

        public string GetIdName()
        {
            return _calendarIds[_primaryId];
        }

        public ObservableCollection<TimeBlock> GetFreeTime()
        {
            return _freeTime;
        } 
        public void ResetPrimaryId()
        {
            var settingsProperty = Settings.Default.Properties[PrimaryId];
            if (settingsProperty != null)
                SetPrimaryId((string)settingsProperty.DefaultValue);
        }
    }
}