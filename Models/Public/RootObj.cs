using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace IME.SpotDataApi.Models.Public
{
    public interface IRootObj
    {
        object Key { get; }
        string KeyName { get; }
    }

    public abstract class RootObj<TKey> : IRootObj
    {
        /// <summary> شناسه </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public virtual TKey Id { get; set; }

        [JsonIgnore] public virtual string KeyName => nameof(Id);

        [JsonIgnore] object IRootObj.Key => Id;
    }

    public abstract class RootObjR<TKey> : IRootObj
    {
        public abstract TKey Id { get; }

        [JsonIgnore] public virtual string KeyName => nameof(Id);

        [JsonIgnore] object IRootObj.Key => Id;
    }
}