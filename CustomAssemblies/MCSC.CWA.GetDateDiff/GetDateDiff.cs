using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System.Activities;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;

namespace MCSC.CWA.GetDateDiff
{
    public class GetDateDiff : CodeActivity
    {
        [RequiredArgument]
        [Input("Date 1")]
        public InArgument<DateTime> Date1 { get; set; }
        [Input("Date 2")]
        public InArgument<DateTime> Date2 { get; set; }
        [Output("Total Days")]
        public OutArgument<double> TotalDays { get; set; }
        [Output("Total Hours")]
        public OutArgument<double> TotalHours { get; set; }
        [Output("Total Milliseconds")]
        public OutArgument<double> TotalMilliseconds { get; set; }
        [Output("Total Minutes")]
        public OutArgument<double> TotalMinutes { get; set; }
        [Output("Total Seconds")]
        public OutArgument<double> TotalSeconds { get; set; }
        [Output("Day Of Week")]
        public OutArgument<int> DayOfWeek { get; set; }
        [Output("Day Of Year")]
        public OutArgument<int> DayOfYear { get; set; }
        [Output("Day")]
        public OutArgument<int> Day { get; set; }
        [Output("Month")]
        public OutArgument<int> Month { get; set; }
        [Output("Year")]
        public OutArgument<int> Year { get; set; }
        [Output("Week Of Year")]
        public OutArgument<int> WeekOfYear { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            TimeSpan difference = new TimeSpan();
            int DayOfWeek = 0;
            int DayOfYear = 0;
            int Day = 0;
            int Month = 0;
            int Year = 0;
            int WeekOfYear = 0;

            DateTime date1 = Date1.Get(executionContext);
            DateTime date2 = Date2.Get(executionContext);

            GetDateDifference(date1, date2, ref difference, ref DayOfWeek, ref DayOfYear, ref Day, ref Month, ref Year, ref WeekOfYear);

            TotalDays.Set(executionContext, difference.TotalDays);
            TotalHours.Set(executionContext, difference.TotalHours);
            TotalMilliseconds.Set(executionContext, difference.TotalMilliseconds);
            TotalMinutes.Set(executionContext, difference.TotalMinutes);
            TotalSeconds.Set(executionContext, difference.TotalSeconds);

            this.DayOfWeek.Set(executionContext, DayOfWeek);
            this.DayOfYear.Set(executionContext, DayOfYear);
            this.Day.Set(executionContext, Day);
            this.Month.Set(executionContext, Month);
            this.Year.Set(executionContext, Year);
            this.WeekOfYear.Set(executionContext, WeekOfYear);

        }
        public bool GetDateDifference(DateTime date1, DateTime date2, ref TimeSpan difference, ref int DayOfWeek, ref int DayOfYear, ref int Day, ref int Month, ref int Year, ref int WeekOfYear)
        {
            difference = date1 - date2;
            DayOfWeek = (int)date1.DayOfWeek;
            DayOfYear = date1.DayOfYear;
            Day = date1.Day;
            Month = date1.Month;
            Year = date1.Year;
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            Calendar cal = dfi.Calendar;
            WeekOfYear = cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            return true;
        }
    }
}
