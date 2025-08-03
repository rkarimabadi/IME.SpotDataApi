using System;

namespace IME.SpotDataApi.Models.Public
{
    /// <summary> اطلاعات پایه </summary>
    public class BaseInfo : RootObj<int>, ICloneable
    {
        /// <summary> توضیحات </summary>
        public string Description { get; set; }

        /// <summary> نام فارسی </summary>
        public string PersianName { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override bool Equals(object? obj)
        {
            return obj is BaseInfo info &&
                   Id == info.Id;
        }
    }
}