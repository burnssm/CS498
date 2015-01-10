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
            var tasks = new List<Task>();
            for (var i = 0; i < 100; i++)
            {
                tasks.Add(new Task {Name = "Hello" + i, Date = new DateTime().ToLongTimeString()});
            }

            TaskList.ItemsSource = tasks;
            GoogleList.ItemsSource = tasks;
        }

        private class Task
        {
            public string Name { get; set; }
            public string Date { get; set; }
            
        }
    }
}
