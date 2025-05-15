namespace BlazorWAemail.Server.Models
{
    public class DbServerInfo
    {
        public string CurrentUser { get; set; }
        public string AuthType { get; set; }
        public string EncryptionType { get; set; }
        public string ClientIp { get; set; }
        public DateTime LoginDate { get; set; }
        public string ServerVersion { get; set; }
        public DateTime ServerStartTime { get; set; }
        public string ServerName { get; set; }
        public string CurrentDatabase { get; set; }
    }
}
