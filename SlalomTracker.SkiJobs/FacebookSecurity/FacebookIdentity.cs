using System.Security.Principal;

namespace SlalomTracker.SkiJobs.FacebookSecurity
{
    public class FacebookIdentity : IIdentity
    {
        public string AuthenticationType { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public string Name { get; private set; }        

        public FacebookIdentity(DebugToken accessToken)
        {
            this.IsAuthenticated = accessToken.data.is_valid;
            this.Name = accessToken.data.user_id;
            this.AuthenticationType = "FacebookToken";
        }
    }
}