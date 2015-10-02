﻿using System.ComponentModel.DataAnnotations;

namespace Mvc.Server.Models {
    public class Application {
        [Key]
        public string ApplicationID { get; set; }
        public string DisplayName { get; set; }
        public string RedirectUri { get; set; }
        public string LogoutRedirectUri { get; set; }
        public string Secret { get; set; }
        public bool RequireUserConsent { get; set; }
    }
}