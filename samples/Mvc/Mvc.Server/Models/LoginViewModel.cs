namespace Mvc.Server.Models
{
    public class LoginViewModel
    {
        public LoginViewModel(string uniqueId, string error = null)
        {
            Error = error;
            UniqueId = uniqueId;
        }

        public string Error { get; set; }
        public string UniqueId { get; private set; }
    }
}