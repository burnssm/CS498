using System;
using System.Collections.Generic;

namespace CS498
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            AddDummyTasks();
        }

        private void AddDummyTasks()
        {
            var tasks = new List<Task>();
            for (var i = 0; i < 100; i++)
            {
                tasks.Add(new Task() {Name = "Hello" + i, Date = new DateTime().ToLongTimeString()});
            }
            TaskList.ItemsSource = null;
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
