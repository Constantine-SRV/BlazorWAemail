﻿using System.ComponentModel.DataAnnotations;

namespace BlazorWAemail.Server.Models
{
    public class AuthCodeHistory
    {
        [Key]
        public int Id { get; set; } // Primary key

        public string Email { get; set; } // User's email
        public string Code { get; set; } // Generated code

        public string ClientIp { get; set; } // Client's IP address when requesting the code
        public DateTime RequestedAt { get; set; } // Time when the code was requested
        public DateTime? AuthorizedAt { get; set; } // Time when the code was used for authorization, nullable if not used
    }
}
