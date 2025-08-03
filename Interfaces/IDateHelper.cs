namespace IME.SpotDataApi.Interfaces
{
    public interface IDateHelper
    {
        string GetPersianYYYYMMDD(DateTime date);
        (int, int, int) GetPersianDateTupple(DateTime date);
    }
}