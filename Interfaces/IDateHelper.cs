namespace IME.SpotDataApi.Interfaces
{
    public interface IDateHelper
    {
        string GetPersianYYYYMMDD(DateTime date);
        string GetPersian(DateTime date);
        (int, int, int) GetPersianDateTupple(DateTime date);
         DateTime GetGregorian(string persianDate);
    }
}