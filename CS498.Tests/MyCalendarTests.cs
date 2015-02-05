using System;
using System.Linq;
using System.Threading.Tasks;
using CS498.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS498.Tests
{
    [TestClass]
    public class MyCalendarTests
    {
        private DateTime _now;

        [TestInitialize]
        public void Initialize()
        {
            _now = DateTime.Now.AddHours(1);
            MyCalendar.Instance.Authorize().Wait();
        }

        [TestMethod]
        public async Task AddEventHappy()
        {
            var testEvent = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            var countBefore = MyCalendar.Instance.GetTasks().Count;
            await MyCalendar.Instance.AddEvent(testEvent);
            var countAfter = MyCalendar.Instance.GetTasks().Count;
            Assert.AreEqual(countBefore + 1, countAfter);
            await MyCalendar.Instance.DeleteEvent(testEvent);
        }

        [TestMethod]
        public async Task AddEventNullsExceptTimeBlock()
        {
            var testEvent = new GoogleEvent
            {
                Title = null,
                TimeBlock = new TimeBlock(_now.AddHours(1), _now.AddHours(2)),
                Description = null,
                Location = null
            };
            var countBefore = MyCalendar.Instance.GetTasks().Count;
            await MyCalendar.Instance.AddEvent(testEvent);
            var countAfter = MyCalendar.Instance.GetTasks().Count;
            Assert.AreEqual(countBefore + 1, countAfter);
            await MyCalendar.Instance.DeleteEvent(testEvent);
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
            await MyCalendar.Instance.AddEvent(testEvent);
            Assert.Fail("Exeption should have been thrown for null properties of gEvent");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddEventNullEvent()
        {
            await MyCalendar.Instance.AddEvent(null);
            Assert.Fail("Exeption should have been thrown for null properties of gEvent");
        }
        
        [TestMethod]
        public void FreeTimeConflictCheckNothingAdded()
        {
            var tasks = MyCalendar.Instance.GetTasks();
            var freeTime = MyCalendar.Instance.GetFreeTime();
            var problems = 0;
            for (var i = 1; i < tasks.Count; i++)
            {
                problems += freeTime.TakeWhile(timeBlock => timeBlock.Start >= tasks[i - 1].TimeBlock.End || timeBlock.End >= tasks[i - 1].TimeBlock.End)
                    .TakeWhile(timeBlock => timeBlock.Start <= tasks[i - 1].TimeBlock.End || timeBlock.End <= tasks[i - 1].TimeBlock.End)
                    .TakeWhile(timeBlock => timeBlock.Start <= tasks[i - 1].TimeBlock.End || timeBlock.End >= tasks[i].TimeBlock.Start).Count();
            }
            Assert.AreEqual(0, problems);
        }

        [TestMethod]
        public async Task FreeTimeConflictCheckOverlappingTasksAdded()
        {
            var tasks = MyCalendar.Instance.GetTasks();
            var freeTime = MyCalendar.Instance.GetFreeTime();
            var firstEvent = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            var secondEvent = TestEventHelper(_now.AddHours(1).AddMinutes(30), _now.AddHours(2).AddMinutes(30));
            await MyCalendar.Instance.AddEvent(firstEvent);
            await MyCalendar.Instance.AddEvent(secondEvent);
            var problems = 0;
            for (var i = 1; i < tasks.Count; i++)
            {
                problems += freeTime.TakeWhile(timeBlock => timeBlock.Start >= tasks[i - 1].TimeBlock.End || timeBlock.End >= tasks[i - 1].TimeBlock.End)
                    .TakeWhile(timeBlock => timeBlock.Start <= tasks[i - 1].TimeBlock.End || timeBlock.End <= tasks[i - 1].TimeBlock.End)
                    .TakeWhile(timeBlock => timeBlock.Start <= tasks[i - 1].TimeBlock.End || timeBlock.End >= tasks[i].TimeBlock.Start).Count();
            }
            await MyCalendar.Instance.DeleteEvent(firstEvent);
            await MyCalendar.Instance.DeleteEvent(secondEvent);
            Assert.AreEqual(0, problems);
        }
        
        [TestMethod]
        public async Task FreeTimeConflictNonOverlappingTasksAdded()
        {
            var tasks = MyCalendar.Instance.GetTasks();
            var freeTime = MyCalendar.Instance.GetFreeTime();
            var firstEvent = TestEventHelper(_now.AddHours(1), _now.AddHours(2));
            var secondEvent = TestEventHelper(_now.AddHours(2), _now.AddHours(3));
            var thirdEvent = TestEventHelper(_now.AddHours(3), _now.AddHours(4));
            await MyCalendar.Instance.AddEvent(firstEvent);
            await MyCalendar.Instance.AddEvent(secondEvent);
            await MyCalendar.Instance.AddEvent(thirdEvent);
            var problems = 0;
            for (var i = 1; i < tasks.Count; i++)
            {
                problems += freeTime.TakeWhile(timeBlock => timeBlock.Start >= tasks[i - 1].TimeBlock.End || timeBlock.End >= tasks[i - 1].TimeBlock.End)
                    .TakeWhile(timeBlock => timeBlock.Start <= tasks[i - 1].TimeBlock.End || timeBlock.End <= tasks[i - 1].TimeBlock.End)
                    .TakeWhile(timeBlock => timeBlock.Start <= tasks[i - 1].TimeBlock.End || timeBlock.End >= tasks[i].TimeBlock.Start).Count();
            }
            await MyCalendar.Instance.DeleteEvent(firstEvent);
            await MyCalendar.Instance.DeleteEvent(secondEvent);
            await MyCalendar.Instance.DeleteEvent(thirdEvent);
            Assert.AreEqual(0, problems);
        }

        [TestMethod]
        public void GetTasks()
        {
            var tasks = MyCalendar.Instance.GetTasks();
            Assert.IsNotNull(tasks);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetFreeTimeNoTime()
        {
            MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(0, 0, 0), _now.AddHours(7),
                TimeBlockChoices.Today);
            Assert.Fail("Exception should have been thrown");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetFreeTimeBadTimeSpan()
        {
            var ts = new TimeSpan();
            MyCalendar.Instance.GetFreeTimeBlocks(ts, _now.AddHours(7),
                TimeBlockChoices.Today);
            Assert.Fail("Exception should have been thrown");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetFreeTimeBadDueDate()
        {
            var dt = new DateTime();
            MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), dt,
                TimeBlockChoices.Today);
            Assert.Fail("Exception should have been thrown");
        }

        [TestMethod]
        public void GetFreeTimeHappyToday()
        {
            var freeTime = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddHours(7),
                TimeBlockChoices.Today);
            Assert.IsTrue(freeTime.Count > 0);
        }

        [TestMethod]
        public void GetFreeTimeDueDateLessThanSearchTimeToday()
        {
            var freeTimeDueDateBigger = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(3),
                TimeBlockChoices.Today).Count;
            var freeTimeDueDateSame = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(1),
                TimeBlockChoices.Today).Count;
            Assert.AreEqual(freeTimeDueDateBigger, freeTimeDueDateSame);
        }
        
        [TestMethod]
        public void GetFreeTimeHappyTomorrow()
        {
            var freeTime = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(1).AddHours(12),
                TimeBlockChoices.Tomorrow);
            Assert.IsTrue(freeTime.Count > 0);
        }

        [TestMethod]
        public void GetFreeTimeDueDateLessThanSearchTimeTomorrow()
        {
            var freeTimeDueDateBigger = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(40),
                TimeBlockChoices.Tomorrow).Count;
            var freeTimeDueDateSame = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(2),
                TimeBlockChoices.Tomorrow).Count;
            Assert.AreEqual(freeTimeDueDateBigger, freeTimeDueDateSame);
        }
        
        [TestMethod]
        public void GetFreeTimeHappyWeek()
        {
            var freeTime = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(6),
                TimeBlockChoices.FullWeek);
            Assert.IsTrue(freeTime.Count > 0);
        }

        [TestMethod]
        public void GetFreeTimeDueDateLessThanSearchTimeWeek()
        {
            var freeTimeDueDateBigger = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(40),
                TimeBlockChoices.FullWeek).Count;
            var freeTimeDueDateSame = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(7),
                TimeBlockChoices.FullWeek).Count;
            Assert.AreEqual(freeTimeDueDateBigger, freeTimeDueDateSame);
        }

        [TestMethod]
        public void GetFreeTimeHappyTwoWeeks()
        {
            var freeTime = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(7),
                TimeBlockChoices.TwoWeeks);
            Assert.IsTrue(freeTime.Count > 0);
        }

        [TestMethod]
        public void GetFreeTimeDueDateLessThanSearchTimeTwoWeeks()
        {
            var freeTimeDueDateBigger = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(40),
                TimeBlockChoices.TwoWeeks).Count;
            var freeTimeDueDateSame = MyCalendar.Instance.GetFreeTimeBlocks(new TimeSpan(1, 0, 0), _now.AddDays(14),
                TimeBlockChoices.TwoWeeks).Count;
            Assert.AreEqual(freeTimeDueDateBigger, freeTimeDueDateSame);
        }

        [TestMethod]
        public void GetSetPrimaryIds()
        {
            Assert.IsNotNull(MyCalendar.Instance.GetAllIds().Result);
            var primaryIdList = MyCalendar.Instance.GetAllIds().Result;
            foreach (var id in primaryIdList)
            {
                MyCalendar.Instance.SetPrimaryId(id.Key);
                Assert.AreEqual(MyCalendar.Instance.GetIdName(), id.Value);
                Assert.AreEqual(MyCalendar.Instance.GetIdName(), primaryIdList[id.Key]);
            }
            MyCalendar.Instance.ResetPrimaryId();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetPrimaryIdsBadId()
        {
            MyCalendar.Instance.SetPrimaryId("notakey");
            Assert.Fail("Exception should have been thrown. That key definitely doesn't exist.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetPrimaryIdsNullId()
        {
            MyCalendar.Instance.SetPrimaryId(null);
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
