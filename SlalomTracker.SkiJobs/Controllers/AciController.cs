using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Management.ContainerInstance;
using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Rest;
using SlalomTracker.SkiJobs.Models;

namespace SlalomTracker.SkiJobs.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AciController : ControllerBase
    {

        private readonly ILogger<AciController> _logger;
        private readonly IConfiguration _config;
        private readonly string _subscriptionId;
        private readonly string _resourceGroupName;
        private readonly ContainerInstanceManagementClient _aciClient;

        public AciController(ILogger<AciController> logger, 
                IConfiguration config, 
                ServiceClientCredentials azureCredentials)
        {
            _logger = logger;
            _config = config;
            _subscriptionId = _config["SUBSCRIPTIONID"];
            _resourceGroupName = _config["ACI_RESOURCEGROUP"] ?? "ski-jobs";

            _aciClient = GetClient(azureCredentials);
        }

        [HttpGet]
        [Route("list")]
        public IEnumerable<SkiJobContainer> ListContainers()
        {
            IEnumerable<SkiJobContainer> containers = null;

            var list = _aciClient.ContainerGroups.List();
            if (list == null || list.Count() < 1)
                containers = new List<SkiJobContainer>();
            else
                containers = list.Select(c => SkiJobContainer.FromContainerGroup(c));
    
            return containers;
        }

        [HttpGet]
        [Route("logs")]
        public string GetLogs(string container)
        {
            string responseBody = "";

            string containerName = container + "-0"; // Default naming convention, only a single container in the group with -0 suffix
            var logs = _aciClient.Container.ListLogs(_resourceGroupName, container, containerName);
            if (logs != null)
                responseBody = logs.Content;
    
            return responseBody;
        }

        [HttpPost]
        [Route("deleteall")]
        public int DeleteAll()
        {
            return 0;
        }

        private ContainerInstanceManagementClient GetClient(ServiceClientCredentials azureCredentials)
        {
            ContainerInstanceManagementClient aciClient = 
                new ContainerInstanceManagementClient(azureCredentials);
            aciClient.SubscriptionId = _subscriptionId; 
            
            return aciClient;           
        }
    }
}
