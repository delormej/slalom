using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.ContainerInstance;
using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.ContainerInstance.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;

namespace SlalomTracker.SkiJobs.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActivatorController : ControllerBase
    {
        private static string SubscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTIONID");

        private readonly ILogger<ActivatorController> _logger;

        public ActivatorController(ILogger<ActivatorController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<SkiJobContainer> Get()
        {
            var cred = GetAzureCredentials();
            ContainerInstanceManagementClient aciClient = new ContainerInstanceManagementClient(cred);
            aciClient.SubscriptionId = SubscriptionId;

            // ContainerRegistryManagementClient acrClient = new ContainerRegistryManagementClient(cred);
            // acrClient.SubscriptionId = "40a293b5-bd26-47ef-acc3-c001a5bfce82";
            // var acrCredentials = acrClient.Registries.ListCredentials("ski", "jasondelAcr");
            var list = aciClient.ContainerGroups.List().Select(c => SkiJobContainer.FromContainerGroup(c));

            return list;
        }

        private ServiceClientCredentials GetAzureCredentials()
        {
            var azureServiceTokenProvider2 = new AzureServiceTokenProvider();
            var task = azureServiceTokenProvider2.GetAccessTokenAsync("https://management.azure.com/");
            task.Wait();
            string accessToken = task.Result;             
            ServiceClientCredentials cred = new TokenCredentials(accessToken);
            return cred;
        }
    }

    public class SkiJobContainer
    {
        public string Image { get; set; }
        public string Name { get; set; }
        public string Video { get; set; }

        public static SkiJobContainer FromContainerGroup(ContainerGroup item)
        {
            if (item.Containers == null || item.Containers.Count < 1 ||
                    item.Containers[0].Command.Count < 2)
                return new SkiJobContainer() { Name = item.Name };

            return new SkiJobContainer() {
                Image = item.Containers[0].Image,
                Name = item.Name,
                Video = item.Containers[0].Command[2]
            };
        }
    }
}
