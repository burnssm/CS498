using System;
using System.Collections.Generic;
using CS498.Lib;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Calendar = CS498.Lib.MyCalendar;


namespace CS498
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ObservableCollection<GoogleEvent> _events;
        private ObservableCollection<TimeBlock> _timeBlock;
        private Dictionary<string, string> _calendarIds; 

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await MyCalendar.Instance.Authorize();
            }
            catch (AggregateException ex)
            {
                foreach (var ie in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + ie.Message);
                }
            }

            _events = MyCalendar.Instance.GetTaskEvents();
            _timeBlock = MyCalendar.Instance.GetFreeTimeBlocks();

            TaskList.ItemsSource = _events;
            GoogleList.ItemsSource = _timeBlock;
            _calendarIds = await MyCalendar.Instance.GetAllIds();
            Calendar.ItemsSource = _calendarIds.Values;
            Calendar.SelectedValue = MyCalendar.Instance.GetIdName();
            GoogleDate.SelectedValue = MyCalendar.Instance.GetDefaultTimeBlockChoice().ToString();
        }

        private async void Save_OnClick(object sender, RoutedEventArgs e)
        {

            var taskName = TaskName.Text;
            var selectedTimeBlock = (TimeBlock) GoogleList.SelectedItem;

            var formDirty = string.IsNullOrWhiteSpace(taskName) || selectedTimeBlock == null;

            if (!formDirty)
            {

                var description = Description.Text;

                var hour = Hours.Value ?? 0;
                var minutes = Minutes.Value ?? 0;

                var newEvent = new GoogleEvent
                {
                    Title = taskName,
                    TimeBlock = new TimeBlock(selectedTimeBlock.Start, selectedTimeBlock.Start.AddHours(hour).AddMinutes(minutes)),
                    Description = description
                };

                await MyCalendar.Instance.AddEvent(newEvent);
                _events = MyCalendar.Instance.GetTasks();
            }
            else
            {
                MessageBox.Show("Please Fill In All Values!",
                    "Invalid Form",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);

            }
            ClearForm();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            TaskName.Clear();
            Description.Text = "";
            DateTimePicker.Text = "";
            GoogleList.ItemsSource = null;
            GoogleList.UnselectAll();
            Hours.Value = 0;
            Minutes.Value = 15;
        }

        private void GoogleDate_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTimeBlock();
        }

        private void HoursOrMinutes_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Minutes == null || Hours == null || Minutes.Value == null || Hours.Value == null) return;

            var minute = Minutes.Value ?? 0;
            var hour = Hours.Value?? 0;

            if (hour == 0 && minute == 0) return;
            UpdateTimeBlock();


            if (StartTime != null && StartTime.Value != null && GoogleList.SelectedValue != null)
            {
                var selectedItemEnd = ((TimeBlock)GoogleList.SelectedValue).End;
                var startTime = (DateTime)StartTime.Value;


                var newTime = startTime.AddHours(hour).AddMinutes(minute);


                if (newTime < selectedItemEnd)
                {
                    EndTime.Content = newTime.ToString("T");
                }
                else
                {
                    GoogleList.SelectedIndex = -1;
                }
            }
        }
        private void DateTimePicker_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateTimeBlock();
        }

        private void UpdateTimeBlock()
        {

            if (GoogleDate == null || GoogleDate.SelectedValue == null) return;
            var googleDate = (TimeBlockChoices)(Enum.Parse(typeof(TimeBlockChoices), (string)GoogleDate.SelectedValue));
            var hoursMinutes = GetHourMinute();
            DateTime timeEnd = DateTime.MaxValue;
            if (DateTimePicker.Value != null)
            {
                 timeEnd = DateTimePicker.Value.Value;
                
            }
            var timeSpan = new TimeSpan(hoursMinutes.Item1, hoursMinutes.Item2, 0);
            GoogleList.ItemsSource = MyCalendar.Instance.GetFreeTimeBlocks(timeSpan, timeEnd, googleDate);
        }

        private Tuple<int, int> GetHourMinute()
        {
            var hour = 0;
            var minutes = 0;
            if (Hours != null)
            {
                hour = Hours.Value ?? 0;
            }
            if (Minutes != null)
            {
                minutes = Minutes.Value ?? 0;
            }
            return new Tuple<int, int>(hour, minutes);
        }

        private void Calendar_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var calendar = e.AddedItems[0] as string;

            var id = _calendarIds.FirstOrDefault(x => x.Value == calendar).Key;
            if (string.IsNullOrEmpty(id)) return;
            MyCalendar.Instance.SetPrimaryId(id);

            if (e.RemovedItems.Count == 0) return;
            _timeBlock = MyCalendar.Instance.GetFreeTimeBlocks();
            _events = MyCalendar.Instance.GetTasks();
        }

        private void ChangeVisibility(bool visibility)
        {
            var visibilityAttribute = visibility ? Visibility.Visible : Visibility.Hidden;
            StartTimeLabel.Visibility = visibilityAttribute;
            StartTime.Visibility = visibilityAttribute;
            EndTime.Visibility = visibilityAttribute;

        }

        private void GoogleList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
            {
                ChangeVisibility(false);
                return;
            }

            var possibleTimeBlock = e.AddedItems[0] as TimeBlock;

            if (possibleTimeBlock == null) return;
            ChangeVisibility(true);
            var minute = Minutes.Value;
            var hour = Hours.Value;

            StartTime.Maximum = null;
            StartTime.Minimum = null;

            var newMax = possibleTimeBlock.End.AddHours((double) -hour).AddMinutes((double) -minute);
            var newMin = possibleTimeBlock.Start;


            StartTime.Value = newMin;
            StartTime.Maximum = newMax;
            StartTime.Minimum = newMin;

        }

        private void StartTime_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var newValue = (DateTime)e.NewValue;

            var minute = Minutes.Value;
            var hour = Hours.Value;

            if (hour != null && minute != null)
                EndTime.Content = newValue.AddHours((double) hour).AddMinutes((double) minute).ToString("T");
        }
    }
}
