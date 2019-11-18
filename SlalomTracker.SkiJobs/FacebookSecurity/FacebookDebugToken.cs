namespace SlalomTracker.SkiJobs.FacebookSecurity
{
    public class DebugToken {
        public DebugTokenData data { get; set; }
    }

    public class DebugTokenData {
        public bool is_valid { get; set; }
        public string user_id { get; set; }
       public DebugTokenError error { get; set; }
    }

    public class DebugTokenError {
        public int code { get; set; }
        public string message { get; set; }
    }
}