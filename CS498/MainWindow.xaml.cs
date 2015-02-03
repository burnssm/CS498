using System;
using System.Collections.Generic;
using System.Linq;
using CS498.Lib;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Calendar = CS498.Lib.Calendar;


namespace CS498
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ObservableCollection<GoogleEvent> _events;
        private ObservableCollection<OpenTimeBlocks> _timeBlocks;

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
            GoogleDate.ItemsSource = MyCalendar.Instance.GetAllIds().Values.ToList().OrderBy(x => x);
            GoogleDate.SelectedValue = MyCalendar.Instance.GetIdName();
        }

        private void TaskDates_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var date = e.AddedItems[0] as string;

            //TODO Check if Calendar is in list or get it
        }

        private void GoogleDate_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var calendar = e.AddedItems[0] as string;
            //TODO Get TimeBlocks for the selected time
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            var taskName = TaskName.Text;
            var description = Description.Text;

            var selectedTimeBlock = (OpenTimeBlocks)GoogleList.SelectedItem;

            var hour = Hours.Value ?? 0;
            var minutes = Minutes.Value ?? 0;

            if (selectedTimeBlock != null)
            {
                var newEvent = new GoogleEvent
                {
                    Title = taskName,
                    StartDateTime = selectedTimeBlock.StartDateTime,
                    EndDateTime = selectedTimeBlock.StartDateTime.AddHours(hour).AddMinutes(minutes),
                    Description = description
                };

                _events.Add(newEvent);
                //TODO Create the event on the calendar
            }
            else
            {
                //TODO Make own Form and Position in Center if we have time
                MessageBox.Show("Please Select a Time Block!",
                    "Invalid Form",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);

            }
            ClearForm();
        }

        private void ClearForm()
        {

            TaskName.Clear();
            Description.Text = "";
            DateTimePicker.Text = "";
            GoogleList.UnselectAll();
            Hours.Value = 0;
            Minutes.Value = 0;
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void HoursOrMinutes_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var hour = Hours.Value ?? 0;
            var minute = Minutes.Value ?? 0;

            //TODO Get time blocks for the amount of hours and minutes
        }
    }
}
