using System.ComponentModel.DataAnnotations;

namespace BlazorWAemail.Server.Models
{
    public class AppSetting
    {
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}