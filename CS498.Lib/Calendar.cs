﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;
using CalendarService = Google.Apis.Calendar.v3.CalendarService;

namespace CS498.Lib
{
    public class Calendar
    {
        public static void Authorize()
        {
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = PrivateConsts.ClientId,
                    ClientSecret = PrivateConsts.ClientSecrets
                },
                new[] { CalendarService.Scope.Calendar },
                "user",
                CancellationToken.None,
                new FileDataStore("Calendar.Auth.Store")).Result;

            // Create the service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Calendar API Sample",
            });

            var query = new CalendarQuery
            {
                Uri = new Uri("https://www.google.com/calendar/feeds/default/allcalendars/full")
            };
            var x = service.CalendarList.Get("primary").Execute();
            Console.Out.WriteLine(x.Description);
            Console.Out.WriteLine(x.Kind);
            Console.Out.WriteLine(x.Location);
            Console.Out.WriteLine(x.Summary);
        }

        public static void AddEvent(CalendarService service, string title, string contents, string location,
            DateTime startTime, DateTime endTime)
        {
        }
    }
}
