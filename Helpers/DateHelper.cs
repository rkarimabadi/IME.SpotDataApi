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
        public (int, int, int) GetPersianDateTupple(DateTime date)
        {
            return (pc.GetYear(date), pc.GetMonth(date), pc.GetDayOfMonth(date));
        }
    }
}
