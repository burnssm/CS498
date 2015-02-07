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
        private readonly CalendarController _calendarController;

        public MainWindow()
        {
            InitializeComponent();
            _calendarController = new CalendarController();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _events = await _calendarController.GetTasks();
            _calendarIds = await _calendarController.GetCalendarIds();
            Calendar.SelectedValue = _calendarController.GetIdName();

            TaskList.ItemsSource = _events;
            GoogleList.ItemsSource = _timeBlock;
            Calendar.ItemsSource = _calendarIds.Values;
        }

        private async void Save_OnClick(object sender, RoutedEventArgs e)
        {
            var taskName = TaskName.Text;

            var hourMinute = GetHourMinute();
            var hour = hourMinute.Item1;
            var minutes = hourMinute.Item2;

            var isFormDirty = string.IsNullOrWhiteSpace(taskName) || 
                            (StartTime == null || StartTime.Value == null || !StartTime.IsVisible) ||
                            (minutes == 0 && hour == 0);

            if (!isFormDirty)
            {

                var description = Description.Text;
                var location = Location.Text;
                var startTime = (DateTime)StartTime.Value;

                var newEvent = new GoogleEvent
                {
                    Title = taskName,
                    TimeBlock = new TimeBlock(startTime, startTime.AddHours(hour).AddMinutes(minutes)),
                    Description = description,
                    Location = location
                };

                await _calendarController.AddEvent(newEvent);
                _timeBlock = _calendarController.GetFreeTime();
                ClearForm();
            }
            else
            {
                MessageBox.Show("Please Fill In All Values!",
                    "Invalid Form",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            TaskName.Clear();
            Location.Clear();
            Description.Clear();
            DateTimePicker.Text = "";
            GoogleList.ItemsSource = null;
            GoogleList.UnselectAll();
            GoogleDate.SelectedIndex = -1;
            Hours.Value = 0;
            Minutes.Value = 0;
        }

        private void GoogleDate_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTimeBlock();
        }

        private void HoursOrMinutes_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var hoursMinutes = GetHourMinute();
            var hour = hoursMinutes.Item1;
            var minute = hoursMinutes.Item2;

            UpdateTimeBlock();
            var googleItem = GoogleList == null ? null : GoogleList.SelectedValue as TimeBlock;
            if (googleItem == null) return;
            DisplayStartAndEndDate(hour, minute, googleItem);

            if (StartTime == null || StartTime.Value == null) return;
            var selectedItemEnd = googleItem.End;
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
        private void DateTimePicker_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateTimeBlock();
        }

        private void UpdateTimeBlock()
        {

            if (GoogleDate == null || GoogleDate.SelectedValue == null) return;
            var googleDate = (TimeBlockChoices)(Enum.Parse(typeof(TimeBlockChoices), (string)GoogleDate.SelectedValue));
            var hoursMinutes = GetHourMinute();
            var timeEnd = DateTime.MaxValue;
            if (DateTimePicker.Value != null)
            {
                 timeEnd = DateTimePicker.Value.Value;
                
            }
            var timeSpan = new TimeSpan(hoursMinutes.Item1, hoursMinutes.Item2, 0);
            GoogleList.ItemsSource = _calendarController.GetFreeTimeBlocks(timeSpan, timeEnd, googleDate);
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

            if (hour == 0 && minutes == 0)
            {
                ChangeVisibility(false);
            }
            return new Tuple<int, int>(hour, minutes);
        }

        private void DisplayStartAndEndDate(int hour, int minutes, TimeBlock possibleTimeBlock)
        {
            var visibility = !(hour == 0 && minutes == 0);
            ChangeVisibility(visibility);
            StartTime.Maximum = null;
            StartTime.Minimum = null;

            var newMax = possibleTimeBlock.End.AddHours(-hour).AddMinutes(-minutes);
            var newMin = possibleTimeBlock.Start;

            StartTime.Value = newMin;
            StartTime.Maximum = newMax;
            StartTime.Minimum = newMin;
        }

        private async void Calendar_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var calendar = e.AddedItems[0] as string;

            var id = _calendarIds.FirstOrDefault(x => x.Value == calendar).Key;
            if (string.IsNullOrEmpty(id)) return;
            await _calendarController.SetPrimaryId(id);

            if (e.RemovedItems.Count == 0) return;
            _events = await _calendarController.GetTasks();
            _timeBlock = _calendarController.GetFreeTime();
        }

        private void ChangeVisibility(bool isVisible)
        {
            if (StartTimeLabel == null || StartTime == null || EndTime == null) return;
            var visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            StartTimeLabel.Visibility = visibility;
            StartTime.Visibility = visibility;
            EndTime.Visibility = visibility;

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
            var hourMinutes = GetHourMinute();
            DisplayStartAndEndDate(hourMinutes.Item1, hourMinutes.Item2, possibleTimeBlock);
        }

        private void StartTime_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var newValue = (DateTime) e.NewValue;

            var hourMinute = GetHourMinute();
            EndTime.Content = newValue.AddHours(hourMinute.Item1).AddMinutes(hourMinute.Item2).ToString("T");
        }
    }
}
