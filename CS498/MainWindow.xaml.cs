using System;
using System.Collections.Generic;
using CS498.Lib;
using System;


namespace CS498
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            try
            {
                MyCalendar.Instance.Authorize().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
            InitializeComponent();
            AddDummyTasks();
            MyCalendar.Instance.GetFreeTime();
        }

        private void AddDummyTasks()
        {
            var tasks = new List<GoogleEvent>();
            var timeblocks = new List<TimeBlock>();
            for (var i = 0; i < 100; i++)
            {
                var timeBlock = new TimeBlock
                {
                    Start = DateTime.Now,
                    End = DateTime.Now.AddHours(2)
                };
                tasks.Add(new GoogleEvent
                {
                    Title = "Hello" + i,
                    TimeBlock = timeBlock,
                    Description = "Test"
                });
                timeblocks.Add(timeBlock);
            }

            TaskList.ItemsSource = tasks;
            GoogleList.ItemsSource = timeblocks;
        }

    }
}
