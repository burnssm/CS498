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
            var x = MyCalendar.Instance;
            try
            {
                x.Authorize().Wait();
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
        }

        private void AddDummyTasks()
        {
            var tasks = new List<GoogleEvent>();
            var timeblocks = new List<OpenTimeBlocks>();
            for (var i = 0; i < 100; i++)
            {
                tasks.Add(new GoogleEvent
                {
                    Title = "Hello" + i,
                    StartDateTime = DateTime.Now,
                    EndDateTime = DateTime.Now.AddHours(2),
                    Description = "Test"
                });
                timeblocks.Add(new OpenTimeBlocks
                {
                    StartDateTime = DateTime.Now,
                    EndDateTime = DateTime.Now.AddHours(2)
                });
            }

            TaskList.ItemsSource = tasks;
            GoogleList.ItemsSource = timeblocks;
        }

    }
}
