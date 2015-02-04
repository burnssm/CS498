using System;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void GoogleDate_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTimeBlock();
        }

        private async void Save_OnClick(object sender, RoutedEventArgs e)
        {
            var taskName = TaskName.Text;
            var description = Description.Text;

            var selectedTimeBlock = (TimeBlock)GoogleList.SelectedItem;

            var hour = Hours.Value ?? 0;
            var minutes = Minutes.Value ?? 0;

            if (selectedTimeBlock != null)
            {
                var newEvent = new GoogleEvent
                {
                    Title = taskName,
                    TimeBlock = new TimeBlock(selectedTimeBlock.Start ,selectedTimeBlock.Start.AddHours(hour).AddMinutes(minutes)),
                    Description = description
                };

                MyCalendar.Instance.AddEvent(newEvent);
                _events = await MyCalendar.Instance.GetTasks(MyCalendar.Instance.GetIdName());
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
            GoogleList.ItemsSource = null;
            GoogleList.UnselectAll();
            Hours.Value = 0;
            Minutes.Value = 0;
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            ClearForm();
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
            return new Tuple<int, int>(hour,minutes);
        }

        private void HoursOrMinutes_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateTimeBlock();
        }

        private void UpdateTimeBlock()
        {

            if (GoogleDate == null || GoogleDate.SelectedValue == null) return;
            var googleDate = (TimeBlockChoices)(Enum.Parse(typeof(TimeBlockChoices), (string)GoogleDate.SelectedValue));
            var hoursMinutes = GetHourMinute();
            if (DateTimePicker.Value == null) return;
            var timeEnd = DateTimePicker.Value.Value;
            var timeSpan = new TimeSpan(hoursMinutes.Item1, hoursMinutes.Item2, 0);
            GoogleList.ItemsSource = MyCalendar.Instance.GetFreeTimeBlocks(timeSpan, timeEnd, googleDate);
        }

        private async void Calendar_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var calendar = e.AddedItems[0] as string;

            MyCalendar.Instance.SetPrimaryId(calendar);
            _events = await MyCalendar.Instance.GetTasks(MyCalendar.Instance.GetIdName());
        }

        private void DateTimePicker_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateTimeBlock();
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

            _events = await MyCalendar.Instance.GetTasks(MyCalendar.Instance.GetIdName());
            _timeBlock = new ObservableCollection<TimeBlock>();

            TaskList.ItemsSource = _events;
            GoogleList.ItemsSource = _timeBlock;
            var ids = await MyCalendar.Instance.GetAllIds();
            Calendar.ItemsSource = ids.Values.OrderBy(x => x).ToList();
            Calendar.SelectedValue = MyCalendar.Instance.GetIdName();
        }
    }
}
