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
            var testEvent = new GoogleEvent
            {
                Title = "title",
                TimeBlock = new TimeBlock(_now.AddHours(1), _now.AddHours(2)),
                Description = "desc",
                Location = "loc"
            };
            var countBefore = MyCalendar.Instance.GetTasks().Result.Count;
            await MyCalendar.Instance.AddEvent(testEvent);
            var countAfter = MyCalendar.Instance.GetTasks().Result.Count;
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
            var countBefore = MyCalendar.Instance.GetTasks().Result.Count;
            await MyCalendar.Instance.AddEvent(testEvent);
            var countAfter = MyCalendar.Instance.GetTasks().Result.Count;
            Assert.AreEqual(countBefore + 1, countAfter);
            await MyCalendar.Instance.DeleteEvent(testEvent);
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
        [ExpectedException(typeof(ArgumentNullException))]
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
        public async Task AddEvent()
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
        public void FreeTimeConflictCheckNothingAdded()
        {
            var tasks = MyCalendar.Instance.GetTasks().Result;
            var freeTime = MyCalendar.Instance._freeTime;
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
            var tasks = MyCalendar.Instance.GetTasks().Result;
            var freeTime = MyCalendar.Instance._freeTime;
            await MyCalendar.Instance.AddEvent(TestEventHelper(_now.AddHours(1), _now.AddHours(2)));
            await MyCalendar.Instance.AddEvent(TestEventHelper(_now.AddHours(1).AddMinutes(30), _now.AddHours(2).AddMinutes(30)));
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
        public async Task FreeTimeConflictNonOverlappingTasksAdded()
        {
            var tasks = MyCalendar.Instance.GetTasks().Result;
            var freeTime = MyCalendar.Instance._freeTime;
            await MyCalendar.Instance.AddEvent(TestEventHelper(_now.AddHours(1), _now.AddHours(2)));
            await MyCalendar.Instance.AddEvent(TestEventHelper(_now.AddHours(2), _now.AddHours(3)));
            await MyCalendar.Instance.AddEvent(TestEventHelper(_now.AddHours(3), _now.AddHours(4)));
            var problems = 0;
            for (var i = 1; i < tasks.Count; i++)
            {
                problems += freeTime.TakeWhile(timeBlock => timeBlock.Start >= tasks[i - 1].TimeBlock.End || timeBlock.End >= tasks[i - 1].TimeBlock.End)
                    .TakeWhile(timeBlock => timeBlock.Start <= tasks[i - 1].TimeBlock.End || timeBlock.End <= tasks[i - 1].TimeBlock.End)
                    .TakeWhile(timeBlock => timeBlock.Start <= tasks[i - 1].TimeBlock.End || timeBlock.End >= tasks[i].TimeBlock.Start).Count();
            }
            Assert.AreEqual(0, problems);
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
