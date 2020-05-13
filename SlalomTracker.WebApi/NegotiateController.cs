using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;

namespace SlalomTracker.WebApi.Controllers
{
    [ApiController]
    public class NegotiateController : Controller
    {
        const string ENV_SIGNALR = "SKISIGNALR";
        private readonly IServiceManager _serviceManager;
        
        public NegotiateController(IConfiguration configuration)
        {
            string connectionString = configuration[ENV_SIGNALR];
            _serviceManager = new ServiceManagerBuilder()
                .WithOptions(o => o.ConnectionString = connectionString)
                .Build();
        }

        [HttpPost("/api/{hub}/negotiate")]
        public JsonResult Get(string hub, string user = "default")
        {
            return new JsonResult(new Dictionary<string, string>()
            {
                { "url", _serviceManager.GetClientEndpoint(hub) },
                { "accessToken", _serviceManager.GenerateClientAccessToken(hub, user) }
            });            
        }
    }
}