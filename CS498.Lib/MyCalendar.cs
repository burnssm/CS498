﻿using System;
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
        private readonly List<TimeBlock> _freeTime;
        private readonly List<GoogleEvent> _tasks;

        private MyCalendar()
        {
            _tasks = new List<GoogleEvent>();
            _freeTime = new List<TimeBlock>();
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

            UpdateTasksAndFreeTime();
        }

        private void UpdateTasksAndFreeTime()
        {
            var endTime = DateTime.Now.AddDays(7);
            var lr = _service.Events.List(_primaryId);
            lr.TimeMin = DateTime.Now;
            lr.TimeMax = endTime;
            lr.SingleEvents = true;
            lr.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            var request = lr.Execute();
            var notFreeTime = new List<TimeBlock> { new TimeBlock
            {
                Start = DateTime.Now, 
                End = DateTime.Now
            }};
            foreach (var events in request.Items.Where(events => events.Start.DateTime.GetValueOrDefault() > notFreeTime.Last().End))
            {
                _tasks.Add(new GoogleEvent
                {
                    Title = events.Summary,
                    Description = events.Description,
                    TimeBlock = new TimeBlock
                    {
                        Start = events.Start.DateTime.GetValueOrDefault(), 
                        End = events.End.DateTime.GetValueOrDefault()
                    },
                    Location = events.Location
                });
                notFreeTime.Add(new TimeBlock
                {
                    Start = events.Start.DateTime.GetValueOrDefault(),
                    End = events.End.DateTime.GetValueOrDefault()
                });
            }
            for (var x = 1; x < notFreeTime.Count; x++)
            {
                _freeTime.Add(new TimeBlock
                {
                    Start = notFreeTime[x - 1].End, 
                    End = notFreeTime[x].Start
                });
            }
            if (notFreeTime.Last().End < endTime)
                _freeTime.Add(new TimeBlock
                {
                    Start = notFreeTime.Last().End, 
                    End = endTime
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

        public void AddEvent(GoogleEvent gEvent)
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

            _service.Events.Insert(calendarEvent, _primaryId).Execute();
            foreach (var googleEvent in _tasks.Where(googleEvent => googleEvent.TimeBlock.Start > gEvent.TimeBlock.End))
            {
                _tasks.Insert(_tasks.IndexOf(googleEvent), gEvent);
                break;
            }
            foreach (var timeBlock in _freeTime.Where(x => x.End > gEvent.TimeBlock.Start))
            {
                var newTimeBlock = new TimeBlock
                {
                    Start = gEvent.TimeBlock.End, 
                    End = timeBlock.End
                };
                timeBlock.End = gEvent.TimeBlock.Start;

                var index = timeBlock == _freeTime.Last() ? _freeTime.IndexOf(timeBlock) : _freeTime.IndexOf(timeBlock)+1;

                _freeTime.Insert(index, newTimeBlock);
                break;
            }
        }

        public List<GoogleEvent> GetTasks()
        {
            return _tasks;
        } 

        public List<TimeBlock> GetFreeTime()
        {
            return _freeTime;
        }
    }
}