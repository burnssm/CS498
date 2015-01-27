﻿using System;
using System.Collections.Generic;
using System.Windows;
using CS498.Lib;

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
        }
        private void ComboBox_Loaded(object sender, RoutedEventArgs args)
        {
            var dataList = new List<string>();
            for (var i = 1; i <= 12; i++)
            {
                dataList.Add(i.ToString());
            }

            var box = sender as System.Windows.Controls.ComboBox;
            if (box != null) box.ItemsSource = dataList;
        }
    }
}
