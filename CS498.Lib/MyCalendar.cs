using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.GData.Calendar;
using Google.GData.Extensions;
using CalendarService = Google.Apis.Calendar.v3.CalendarService;

namespace CS498.Lib
{
    public class MyCalendar
    {
        public static void Authorize()
        {

            Console.WriteLine("Discovery API Sample");
            Console.WriteLine("====================");
            try
            {
                Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
        }

        public static async Task Run()
        {
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = PrivateConsts.ClientId,
                    ClientSecret = PrivateConsts.ClientSecrets
                },
                new[] {CalendarService.Scope.Calendar},
                "user",
                CancellationToken.None,
                new FileDataStore("Calendar.Auth.Store")).Result;

            // Create the service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Calendar API Sample",
            });

            var primaryCalendar = service.Freebusy.Query(new FreeBusyRequest());
            var calendar = new Calendar();
            calendar.Id = "Little League Schedule";
            calendar.Summary = "This calendar contains the practice schedule and game times.";
            calendar.TimeZone = "America/Los_Angeles";
            calendar.Location = "Oakland";
            service.Calendars.Insert(calendar);
        }
    }
}
