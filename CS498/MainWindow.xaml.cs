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
        private ObservableCollection<TimeBlock> _freeTime;
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
            _freeTime = new ObservableCollection<TimeBlock>();
            PopulateGoogleList(_calendarController.GetFreeTime());
            Calendar.SelectedValue = _calendarController.GetIdName();

            TaskList.ItemsSource = _events;
            GoogleList.ItemsSource = _freeTime;
            Calendar.ItemsSource = _calendarIds.Values;
            GoogleDate.SelectedValue = _calendarController.GetLengthOfTimeToShow().ToString();
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
                PopulateGoogleList( _calendarController.GetFreeTime());
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
            GoogleList.UnselectAll();
            GoogleDate.SelectedIndex = -1;
            GoogleDate.SelectedValue = _calendarController.GetLengthOfTimeToShow().ToString();
            PopulateGoogleList(_calendarController.GetFreeTime());
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
            //Found at https://msdn.microsoft.com/en-us/library/essfb559%28v=vs.110%29.aspx
            var googleDate = (TimeBlockChoices)(Enum.Parse(typeof(TimeBlockChoices), (string)GoogleDate.SelectedValue));
            var hoursMinutes = GetHourMinute();
            var timeEnd = DateTime.MaxValue;
            if (DateTimePicker.Value != null)
            {
                 timeEnd = DateTimePicker.Value.Value;
                
            }
            var timeSpan = new TimeSpan(hoursMinutes.Item1, hoursMinutes.Item2, 0);
            PopulateGoogleList( _calendarController.GetFreeTime(timeSpan, timeEnd, googleDate));
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
            PopulateGoogleList( _calendarController.GetFreeTime());
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

        private void PopulateGoogleList(IReadOnlyList<TimeBlock> timeBlocks)
        {
            var selectedValue = GoogleList.SelectedValue as TimeBlock;
            var index = -1;
            for( var i = 0; i< timeBlocks.Count; i++)
            {
                if (selectedValue != null && timeBlocks[i].Start == selectedValue.Start)
                {
                    index = i;
                }
            }
            _freeTime.Clear();
            foreach (var timeBlock in timeBlocks)
            {
                _freeTime.Add(timeBlock);
            }
            GoogleList.SelectedIndex = index;
        }
    }
}
