namespace Mvc.Server.Models
{
    public class LoginModel
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public bool RememberMe { get; set; }
        public string Unique_Id { get; set; }
    }
}