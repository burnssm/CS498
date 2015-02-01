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
            InitializeComponent();
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
            MyCalendar.Instance.GetFreeTime();

            TaskList.ItemsSource = MyCalendar.Instance.GetTasks();
            GoogleList.ItemsSource = MyCalendar.Instance.GetFreeTime();
        }

    }
}
