// File: IME.SpotDataApi/Models/Configuration/DataSyncSettings.cs

namespace IME.SpotDataApi.Models.Configuration
{
    public class DataSyncSettings
    {
        public const string SectionName = "DataSyncSettings";

        /// <summary>
        /// فاصله زمانی بین هر چرخه کامل همگام‌سازی (به دقیقه)
        /// </summary>
        public int CycleDelayMinutes { get; set; } = 15;

        /// <summary>
        /// فعال یا غیرفعال کردن همگام‌سازی اطلاعات پایه
        /// Key: نام کلاس مدل (مانند "Broker")
        /// Value: true (فعال) یا false (غیرفعال)
        /// </summary>
        public Dictionary<string, bool> BasicInformation { get; set; } = new();

        /// <summary>
        /// فعال/غیرفعال کردن و تنظیمات بازه زمانی منابع عملیاتی
        /// Key: نام کلاس مدل (مانند "Offer")
        /// </summary>
        public Dictionary<string, OperationalResourceSettings> OperationalResources { get; set; } = new();
    }

    public class OperationalResourceSettings
    {
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// بازه شروع همگام‌سازی (تعداد روز نسبت به امروز)
        /// مثال: 7- به معنی ۷ روز قبل است
        /// </summary>
        public int FromDateDaysOffset { get; set; }

        /// <summary>
        /// بازه پایان همگام‌سازی (تعداد روز نسبت به امروز)
        /// مثال: 25 به معنی ۲۵ روز آینده است
        /// </summary>
        public int ToDateDaysOffset { get; set; }
    }
}