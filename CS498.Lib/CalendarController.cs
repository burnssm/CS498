using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Calendar.v3.Data;
using Settings = CS498.Lib.Properties.Settings;

namespace CS498.Lib
{
    public class CalendarController
    {
        private readonly ObservableCollection<TimeBlock> _freeTime;
        private readonly ObservableCollection<GoogleEvent> _tasks;
        private string _primaryId;
        private const string PrimaryId = "PrimaryId";
        private Dictionary<string, string> _calendarIds; 
        private const TimeBlockChoices LengthofTimeToShow = TimeBlockChoices.TwoWeeks;

        public CalendarController ()
        {
            _tasks = new ObservableCollection<GoogleEvent>();
            _freeTime = new ObservableCollection<TimeBlock>();
            _primaryId = (string)Settings.Default[PrimaryId];

        }

        public async Task<Dictionary<string, string>> GetCalendarIds()
        {
            return _calendarIds ?? (_calendarIds = await MyCalendar.Instance.GetAllCalendarsIdsAsync());
        }

        public ObservableCollection<TimeBlock> GetFreeTimeBlocks(TimeSpan timeSpan, DateTime dueDate, TimeBlockChoices googleDate)
        {
            var filterEndDate = DateTime.Now.AddDays((int)googleDate);
            var end = dueDate < filterEndDate ? dueDate : filterEndDate;
            var updatedTimeBlock = _freeTime
                                    .Where(x => x.Duration >= timeSpan && x.End >= end && x.Start <= end)
                                    .Select(x => new TimeBlock(x.Start, end))
                                    .ToList();
            return new ObservableCollection<TimeBlock>(_freeTime.Where(x => x.Duration >= timeSpan && x.End <= end).Concat(updatedTimeBlock));
        }

        public async Task UpdateTasks(int numberOfDays = (int) LengthofTimeToShow)
        {

            _tasks.Clear();
            var request = await MyCalendar.Instance.GetAllEventsAsync(_primaryId, numberOfDays);
            foreach (var events in request.Where(events => events.End.DateTime.GetValueOrDefault() > DateTime.Now))
            {
                _tasks.Add(new GoogleEvent(events.Id)
                {
                    Title = events.Summary,
                    Description = events.Description,
                    TimeBlock =
                        new TimeBlock(events.Start.DateTime.GetValueOrDefault(), events.End.DateTime.GetValueOrDefault()),
                    Location = events.Location
                });
            }
            CalculateFreeTime(numberOfDays);
        }
        public async Task<ObservableCollection<GoogleEvent>> GetTasks(int numberOfDays = (int)LengthofTimeToShow)
        {
            if (!_tasks.Any()) await UpdateTasks(numberOfDays);
            return _tasks;
        }


        private void CalculateFreeTime(int numberOfDays = (int)LengthofTimeToShow)
        {
            if (!_tasks.Any()) return;
            _freeTime.Clear();
            var endTime = DateTime.Now.AddDays(numberOfDays);
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
            if (gEvent == null) throw new ArgumentNullException("gEvent", "Event can not be null");
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
            gEvent = new GoogleEvent(await MyCalendar.Instance.AddEventToCalendarAsync(_primaryId, calendarEvent));

            if (!_tasks.Any())
                _tasks.Add(gEvent);
            else
            {
                var googleEvent = _tasks.LastOrDefault(x => x.TimeBlock.Start <= gEvent.TimeBlock.Start);
                var index = _tasks.IndexOf(googleEvent) + 1;
                if (googleEvent == null)
                {

                    googleEvent = _tasks.FirstOrDefault(x => x.TimeBlock.Start >= gEvent.TimeBlock.End
                                                                || x.TimeBlock.End <= gEvent.TimeBlock.Start);
                    index = _tasks.IndexOf(googleEvent);
                }
                _tasks.Insert(index, gEvent);
            }

            CalculateFreeTime();
        }
        
        public ObservableCollection<TimeBlock> GetFreeTime()
        {
            return _freeTime;
        }

        public async Task DeleteEvent(GoogleEvent googleEvent)
        {
            await MyCalendar.Instance.DeleteEventFromCalendarAsync(_primaryId, googleEvent);
        }


        public async Task SetPrimaryId(string id)
        {
            if (id == null)
                throw new ArgumentNullException("id", "Null string could not be used as a calendar id.");
            var settingsProperty = Settings.Default.Properties[PrimaryId];
            var calendar = await GetCalendarIds();
            if (settingsProperty == null || (!calendar.ContainsKey(id) && !id.Equals(settingsProperty.DefaultValue)))
                throw new ArgumentException("Invalid ID could not be set.", "id");
            _primaryId = id;
            Settings.Default[PrimaryId] = id;
            Settings.Default.Save();
            await UpdateTasks();
        }
        public string GetIdName()
        {
            return _calendarIds[_primaryId];
        }

        public async Task ResetPrimaryId()
        {
            var settingsProperty = Settings.Default.Properties[PrimaryId];
            if (settingsProperty != null)
                await SetPrimaryId((string)settingsProperty.DefaultValue);
        }
    }
}
