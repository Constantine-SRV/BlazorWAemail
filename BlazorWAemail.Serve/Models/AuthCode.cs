using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorWAemail.Server.Models
{
    public class AuthCode
    {
        [Key]
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime Expiration { get; set; }
    }

}
