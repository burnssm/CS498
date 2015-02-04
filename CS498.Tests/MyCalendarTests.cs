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
        }

        [TestMethod]
        public async Task AddEventHappy()
        {
            await MyCalendar.Instance.Authorize();
            var testEvent = new GoogleEvent
            {
                Title = "title",
                TimeBlock = new TimeBlock(_now.AddHours(1), _now.AddHours(2)),
                Description = "desc",
                Location = "loc"
            };
            await MyCalendar.Instance.AddEvent(testEvent);
            await MyCalendar.Instance.DeleteEvent(testEvent);
        }

        [TestMethod]
        public async Task AddEventNullsExceptTimeBlock()
        {
            await MyCalendar.Instance.Authorize();
            var testEvent = new GoogleEvent
            {
                Title = null,
                TimeBlock = new TimeBlock(_now.AddHours(1), _now.AddHours(2)),
                Description = null,
                Location = null
            };
            await MyCalendar.Instance.AddEvent(testEvent);
            await MyCalendar.Instance.DeleteEvent(testEvent);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddEventNullEverything()
        {
            await MyCalendar.Instance.Authorize();
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
            await MyCalendar.Instance.Authorize();
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
    }
}
