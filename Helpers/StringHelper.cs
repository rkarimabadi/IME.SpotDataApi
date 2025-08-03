using IME.SpotDataApi.Resources;
using System.Collections;
using System.Globalization;

namespace IME.SpotDataApi.Helpers
{
    public static class StringHelper
    {
        public static string GetFirstChracters(this string input, int number)
        {
            // Check if the input is null or empty
            if (string.IsNullOrEmpty(input))
            {
                // Return an empty string
                return "";
            }

            // Check if the input is shorter than 25 characters
            if (input.Length <= number)
            {
                // Return the input as it is
                return input;
            }

            // Return the first 25 characters plus ...
            return input.Substring(0, number) + "...";
        }
        public static string GetRingIcon(this string input)
        {
            // Check if the input is null or empty
            if (string.IsNullOrEmpty(input))
            {
                // Return an empty string
                return "info-circle";
            }
            // Check if the input is shorter than 25 characters
            if (RingIcons.ResourceManager.GetString(input) != null)
            {
                // Return the input as it is
                return RingIcons.ResourceManager.GetString(input);
            }
            return "info-circle";
        }

        public static string AddSlashToYYYYMMDDFormat(this string input)
        {
            return input.Substring(0, 4) + "/" +
                input.Substring(4, 2) + "/" +
                input.Substring(6, 2);
        }
        public static string GetNotificationIcon(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return NotificationIcons.ResourceManager.GetString("پیش فرض");
            }
            foreach (DictionaryEntry item in NotificationIcons.ResourceManager.GetResourceSet(CultureInfo.CurrentCulture, true, true))
            {
                if (input.Contains((string)item.Key)) return (string)item.Value;
            }
            return NotificationIcons.ResourceManager.GetString("پیش فرض");
        }
        public static string GetNotificationType(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "عمومی";
            }
            foreach (DictionaryEntry item in NotificationIcons.ResourceManager.GetResourceSet(CultureInfo.CurrentCulture, true, true))
            {
                if (input.Contains((string)item.Key)) return (string)item.Key;
            }
            return "عمومی";
        }
    }
}
