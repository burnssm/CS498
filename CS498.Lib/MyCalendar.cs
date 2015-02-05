﻿using System;
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
        public readonly ObservableCollection<TimeBlock> FreeTime;
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
            FreeTime = new ObservableCollection<TimeBlock>();
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

            var settingsProperty = Settings.Default.Properties[PrimaryId];
            if (settingsProperty != null && _primaryId.Equals((string)settingsProperty.DefaultValue))
            {
                var calendar = await _service.Calendars.Get(_primaryId).ExecuteAsync();
                SetPrimaryId(calendar.Id);
                
            }
            await GetAllOwnedCalendars();
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
            if(timeSpan.Equals(new TimeSpan(0,0,0))) throw new ArgumentException("Time Span must be more than 0.", "timeSpan");
            var filterEndDate = DateTime.Now.AddDays((int)googleDate);
            var end = dueDate < filterEndDate ? dueDate : filterEndDate;
            var updatedTimeBlock = FreeTime
                                    .Where(x => x.Duration >= timeSpan && x.End >= end && x.Start <= end)
                                    .Select(x => new TimeBlock(x.Start, end))
                                    .ToList();
            return new ObservableCollection<TimeBlock>(FreeTime.Where(x => x.Duration >= timeSpan && x.End <= end).Concat(updatedTimeBlock));
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
            FreeTime.Clear();
            var endTime = DateTime.Now.AddDays((double)LengthOfTimeToDisplay);
            for (var x = 1; x < _tasks.Count; x++)
            {
                if (_tasks[x - 1].TimeBlock.End < _tasks[x].TimeBlock.Start)
                    FreeTime.Add(new TimeBlock(_tasks[x - 1].TimeBlock.End, _tasks[x].TimeBlock.Start));
            }
            if (_tasks.First().TimeBlock.Start >= DateTime.Now)
                FreeTime.Insert(0, new TimeBlock(DateTime.Now, _tasks.First().TimeBlock.Start));

            if (_tasks.Last().TimeBlock.End < endTime)
                FreeTime.Add(new TimeBlock(_tasks.Last().TimeBlock.End, endTime));
        }

        public async Task AddEvent(GoogleEvent gEvent)
        {
            if(gEvent == null || gEvent.TimeBlock == null) throw new ArgumentNullException("gEvent", "Event and the Event's TimeBlock can not be null");
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
        /// THIS IS NOT FINISHED - this does not adjust tasklist/freetime 
        /// as it should had it been completely implemented
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
            _primaryId = id;
            Settings.Default[PrimaryId] = id;
            Settings.Default.Save();
        }
        public string GetIdName()
        {
            return _calendarIds[_primaryId];
        }

        public void ResetPrimaryId()
        {
            var settingsProperty = Settings.Default.Properties[PrimaryId];
            if (settingsProperty != null)
                SetPrimaryId((string)settingsProperty.DefaultValue);
        }
    }
}