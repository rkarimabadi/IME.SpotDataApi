using IME.SpotDataApi.Interfaces;
using System.Globalization;

namespace IME.SpotDataApi.Helpers
{
    public class DateHelper : IDateHelper
    {
        PersianCalendar pc = new PersianCalendar();
        public string GetPersianYYYYMMDD(DateTime date)
        {
            int year = pc.GetYear(date);
            int month = pc.GetMonth(date);
            int day = pc.GetDayOfMonth(date);
            string result = $"{year.ToString("D4")}{month.ToString("D2")}{day.ToString("D2")}";
            return result;
        }
        public string GetPersian(DateTime date)
        {
            int year = pc.GetYear(date);
            int month = pc.GetMonth(date);
            int day = pc.GetDayOfMonth(date);
            string result = $"{year.ToString("D4")}/{month.ToString("D2")}/{day.ToString("D2")}";
            return result;
        }
        public (int, int, int) GetPersianDateTupple(DateTime date)
        {
            return (pc.GetYear(date), pc.GetMonth(date), pc.GetDayOfMonth(date));
        }

                /// <summary>
        /// یک رشته تاریخ شمسی با فرمت yyyy/MM/dd را به DateTime میلادی تبدیل می‌کند.
        /// </summary>
        public DateTime GetGregorian(string persianDate)
        {
            try
            {
                var parts = persianDate.Split('/');
                if (parts.Length != 3) return DateTime.MinValue;

                int year = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);
                int day = int.Parse(parts[2]);

                return pc.ToDateTime(year, month, day, 0, 0, 0, 0);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}
