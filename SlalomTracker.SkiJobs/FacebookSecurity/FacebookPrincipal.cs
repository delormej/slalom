using System.Security.Principal;
using System.Security.Claims;
using System.Linq;

namespace SlalomTracker.SkiJobs.FacebookSecurity
{
    public class FacebookPrincipal : ClaimsPrincipal
    {
        private string[] _admins;
        
        private FacebookIdentity _identity;
        
        public override IIdentity Identity { get { return _identity; }  }
        
        public override bool IsInRole(string role) 
        {
            return (role == "admin" && _admins.Contains(_identity.Name));
        }
        
        public FacebookPrincipal(string[] userAdminIds, DebugToken accessToken)
        {
            _identity = new FacebookIdentity(accessToken);
            _admins =  userAdminIds;
        }
    }
}