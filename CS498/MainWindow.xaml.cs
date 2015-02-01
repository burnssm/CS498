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
            Calendar.Authorize();
            InitializeComponent();
            AddDummyTasks();
        }

        private void AddDummyTasks()
        {
            var tasks = new List<Event>();
            var timeblocks = new List<OpenTimeBlocks>();
            for (var i = 0; i < 100; i++)
            {
                tasks.Add(new Event
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
