namespace BlazorWAemail.Shared;

public class SendCodeRequest
{
    public string Email { get; set; }
}

public class VerifyCodeRequest
{
    public string Email { get; set; }
    public string Code { get; set; }
}

public class AuthResult
{
    public string Token { get; set; }
    public string Email { get; set; }
}
