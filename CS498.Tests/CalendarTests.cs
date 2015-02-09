using System;
using System.Linq;
using System.Threading.Tasks;
using CS498.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS498.Tests
{
    [TestClass]
    public class CalendarTests
    {
        private DateTime _now;
        private CalendarController _calendarController;

        [TestInitialize]
        public void Initialize()
        {
            _now = DateTime.Now.AddHours(1);
            _calendarController = new CalendarController();
        }

        [TestMethod]
        public async Task AddEventHappy()
        {
            var testEvent = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            var tasks = await _calendarController.GetTasks();
            var countBefore = tasks.Count;
            await _calendarController.AddEvent(testEvent);
            var newTasks = await _calendarController.GetTasks();
            var countAfter = newTasks.Count;
            Assert.AreEqual(countBefore + 1, countAfter);
            await _calendarController.DeleteEvent(testEvent);
        }

        [TestMethod]
        public async void AddEventNullsExceptTimeBlock()
        {
            var testEvent = new GoogleEvent
            {
                Title = null,
                TimeBlock = new TimeBlock(_now.AddHours(1), _now.AddHours(2)),
                Description = null,
                Location = null
            };
            var tasks = await _calendarController.GetTasks();
            var countBefore = tasks.Count;
            await _calendarController.AddEvent(testEvent);
            var newTasks = await _calendarController.GetTasks();
            var countAfter = newTasks.Count;
            Assert.AreEqual(countBefore + 1, countAfter);
            await _calendarController.DeleteEvent(testEvent);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddEventNullEverything()
        {
            var testEvent = new GoogleEvent
            {
                Title = null,
                TimeBlock = null,
                Description = null,
                Location = null
            };
            await _calendarController.AddEvent(testEvent);
            Assert.Fail("Exeption should have been thrown for null properties of gEvent");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddEventNullEvent()
        {
            await _calendarController.AddEvent(null);
            Assert.Fail("Exeption should have been thrown for null properties of gEvent");
        }
        
        [TestMethod]
        public async Task FreeTimeConflictCheckNothingAdded()
        {
            var events = await _calendarController.GetTasks();
            var freeTime = _calendarController.GetFreeTime();
            var problems = events.Sum(googleEvent => freeTime.Count(timeBlock => (googleEvent.TimeBlock.Start < timeBlock.Start && googleEvent.TimeBlock.End > timeBlock.Start)
                                                                                 || (googleEvent.TimeBlock.Start < timeBlock.End && googleEvent.TimeBlock.Start > timeBlock.Start)
                                                                                 || (googleEvent.TimeBlock.Start <= timeBlock.Start && googleEvent.TimeBlock.End >= timeBlock.End)
                                                                                 || (googleEvent.TimeBlock.Start >= timeBlock.Start && googleEvent.TimeBlock.End <= timeBlock.End)));
            Assert.AreEqual(0, problems);
        }

        [TestMethod]
        public async Task FreeTimeConflictCheckOverlappingTasksAdded()
        {
            var firstEvent = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            var secondEvent = TestEventHelper(_now.AddHours(1).AddMinutes(30), _now.AddHours(2).AddMinutes(30));

            await _calendarController.AddEvent(firstEvent);
            await _calendarController.AddEvent(secondEvent);

            var allTasks = await _calendarController.GetTasks();
            var addedTasks = allTasks.Take(allTasks.IndexOf(secondEvent));
            var freeTime = _calendarController.GetFreeTime();
            var problems = addedTasks.Sum(googleEvent => freeTime.Count(timeBlock => (googleEvent.TimeBlock.Start < timeBlock.Start && googleEvent.TimeBlock.End > timeBlock.Start)
                                                                                 || (googleEvent.TimeBlock.Start < timeBlock.End && googleEvent.TimeBlock.Start > timeBlock.Start)
                                                                                 || (googleEvent.TimeBlock.Start <= timeBlock.Start && googleEvent.TimeBlock.End >= timeBlock.End)
                                                                                 || (googleEvent.TimeBlock.Start >= timeBlock.Start && googleEvent.TimeBlock.End <= timeBlock.End)));
            await _calendarController.DeleteEvent(firstEvent);
            await _calendarController.DeleteEvent(secondEvent);
            Assert.AreEqual(0, problems);
        }
        
        [TestMethod]
        public async Task FreeTimeConflictNonOverlappingTasksAdded()
        {

            var firstEvent = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            var secondEvent = TestEventHelper(_now.AddHours(2), _now.AddHours(3));
            var thirdEvent = TestEventHelper(_now.AddHours(3), _now.AddHours(4));

            await _calendarController.AddEvent(firstEvent);
            await _calendarController.AddEvent(secondEvent);
            await _calendarController.AddEvent(thirdEvent);

            var allTasks = await _calendarController.GetTasks();
            var addedTasks = allTasks.Take(allTasks.IndexOf(thirdEvent));
            var freeTime = _calendarController.GetFreeTime();
            var problems = addedTasks.Sum(googleEvent => freeTime.Count(timeBlock => (googleEvent.TimeBlock.Start < timeBlock.Start && googleEvent.TimeBlock.End > timeBlock.Start)
                                                                                 || (googleEvent.TimeBlock.Start < timeBlock.End && googleEvent.TimeBlock.Start > timeBlock.Start)
                                                                                 || (googleEvent.TimeBlock.Start <= timeBlock.Start && googleEvent.TimeBlock.End >= timeBlock.End)
                                                                                 || (googleEvent.TimeBlock.Start >= timeBlock.Start && googleEvent.TimeBlock.End <= timeBlock.End)));

            await _calendarController.DeleteEvent(firstEvent);
            await _calendarController.DeleteEvent(secondEvent);
            await _calendarController.DeleteEvent(thirdEvent);
            Assert.AreEqual(0, problems);
        }

        [TestMethod]
        public async Task GetTasks()
        {
            var tasks = await _calendarController.GetTasks();
            Assert.IsNotNull(tasks);
        }

        [TestMethod]
        public async Task GetFreeTimeHappyToday()
        {
            await _calendarController.GetTasks();
            var freeTime = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddHours(7),
                TimeBlockChoices.Today);
            Assert.IsTrue(freeTime.Count > 0);
        }

        [TestMethod]
        public async Task GetFreeTimeDueDateLessThanSearchTimeToday()
        {
            var testEventToday = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            await _calendarController.AddEvent(testEventToday);

            var testEventTomorrow = TestEventHelper(_now.AddHours(1).AddDays(1), _now.AddHours(2).AddDays(1));
            await _calendarController.AddEvent(testEventTomorrow);

            var freeTimeDueDateBigger = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(3),
                TimeBlockChoices.Today).Count;

            var freeTimeDueDateSame = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(1),
                TimeBlockChoices.Today).Count;
            Assert.AreEqual(freeTimeDueDateBigger, freeTimeDueDateSame);
            await _calendarController.DeleteEvent(testEventToday);
            await _calendarController.DeleteEvent(testEventTomorrow);

        }
        
        [TestMethod]
        public async Task GetFreeTimeHappyTomorrow()
        {
            await _calendarController.GetTasks();
            var freeTime = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(1).AddHours(12),
                TimeBlockChoices.Tomorrow);
            Assert.IsTrue(freeTime.Count > 0);
        }

        [TestMethod]
        public async Task GetFreeTimeDueDateLessThanSearchTimeTomorrow()
        {
            var testEventToday = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            await _calendarController.AddEvent(testEventToday);

            var testEventTomorrow = TestEventHelper(_now.AddHours(1).AddDays(2), _now.AddHours(2).AddDays(2));
            await _calendarController.AddEvent(testEventTomorrow);

            var freeTimeDueDateBigger = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(40),
                TimeBlockChoices.Tomorrow).Count;
            var freeTimeDueDateSame = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(3),
                TimeBlockChoices.Tomorrow).Count;
            Assert.AreEqual(freeTimeDueDateBigger, freeTimeDueDateSame);

            await _calendarController.DeleteEvent(testEventToday);
            await _calendarController.DeleteEvent(testEventTomorrow);
        }
        
        [TestMethod]
        public async Task GetFreeTimeHappyWeek()
        {
            await _calendarController.GetTasks();
            var freeTime = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(6),
                TimeBlockChoices.FullWeek);
            Assert.IsTrue(freeTime.Count > 0);
        }

        [TestMethod]
        public async Task GetFreeTimeDueDateLessThanSearchTimeWeek()
        {

            var testEventToday = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            await _calendarController.AddEvent(testEventToday);

            var testEvent20Days = TestEventHelper(_now.AddHours(1).AddDays(2), _now.AddHours(2).AddDays(2));
            await _calendarController.AddEvent(testEvent20Days);

            var freeTimeDueDateBigger = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(40),
                TimeBlockChoices.FullWeek).Count;
            var freeTimeDueDateSame = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(7),
                TimeBlockChoices.FullWeek).Count;
            Assert.AreEqual(freeTimeDueDateBigger, freeTimeDueDateSame);

            await _calendarController.DeleteEvent(testEventToday);
            await _calendarController.DeleteEvent(testEvent20Days);
        }

        [TestMethod]
        public async Task GetFreeTimeHappyTwoWeeks()
        {
            await _calendarController.GetTasks();
            var freeTime = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(7),
                TimeBlockChoices.TwoWeeks);
            Assert.IsTrue(freeTime.Count > 0);
        }

        [TestMethod]
        public void GetFreeTimeDueDateLessThanSearchTimeTwoWeeks()
        {
            var freeTimeDueDateBigger = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(40),
                TimeBlockChoices.TwoWeeks).Count;
            var freeTimeDueDateSame = _calendarController.GetFreeTime(new TimeSpan(1, 0, 0), _now.AddDays(14),
                TimeBlockChoices.TwoWeeks).Count;
            Assert.AreEqual(freeTimeDueDateBigger, freeTimeDueDateSame);
        }

        [TestMethod]
        public async Task GetSetPrimaryIds()
        {
            Assert.IsNotNull(await _calendarController.GetCalendarIds());
            var primaryIdList = await _calendarController.GetCalendarIds();
            foreach (var id in primaryIdList)
            {
                await _calendarController.SetPrimaryId(id.Key);
                Assert.AreEqual(await _calendarController.GetIdName(), id.Value);
                Assert.AreEqual(await _calendarController.GetIdName(), primaryIdList[id.Key]);
            }
            await _calendarController.ResetPrimaryId();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task SetPrimaryIdsBadId()
        {
            await _calendarController.SetPrimaryId("notakey");
            Assert.Fail("Exception should have been thrown. That key definitely doesn't exist.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SetPrimaryIdsNullId()
        {
            await _calendarController.SetPrimaryId(null);
            Assert.Fail("Exception should have been thrown. That key definitely doesn't exist.");
        }

        private static GoogleEvent TestEventHelper(DateTime start, DateTime end)
        {
            var testEvent = new GoogleEvent
            {
                Title = "title",
                TimeBlock = new TimeBlock(start, end),
                Description = "desc",
                Location = "loc"
            };
            return testEvent;
        }
        
    }
}
