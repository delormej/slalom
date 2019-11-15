using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Management.ContainerInstance;
using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using SlalomTracker.SkiJobs.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace SlalomTracker.SkiJobs.Controllers
{
    [ApiController]
    [Authorize(Roles = "admin")]
    [Route("[controller]")]
    public class AciController : ControllerBase
    {

        private readonly ILogger<AciController> _logger;
        private readonly IConfiguration _config;
        private readonly string _subscriptionId;
        private readonly string _jobsResourceGroupName;
        private readonly ContainerInstanceManagementClient _aciClient;

        public AciController(ILogger<AciController> logger, 
                IConfiguration config, 
                ServiceClientCredentials azureCredentials)
        {
            _logger = logger;
            _config = config;
            _subscriptionId = _config["SUBSCRIPTIONID"];
            _jobsResourceGroupName = _config["ACI_RESOURCEGROUP"] ?? "ski-jobs";

            _aciClient = GetClient(azureCredentials);
        }

        [HttpGet]
        [Route("list")]
        public IEnumerable<SkiJobContainer> ListContainers()
        {
            IEnumerable<SkiJobContainer> containers = null;

            var list = _aciClient.ContainerGroups.ListByResourceGroup(_jobsResourceGroupName);
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
            var logs = _aciClient.Container.ListLogs(_jobsResourceGroupName, container, containerName);
            if (logs != null)
                responseBody = logs.Content;
    
            return responseBody;
        }

        [HttpPost]
        [Route("create")]
        public IActionResult Create([FromBody]string videoUrl)
        {
            try
            {
                SkiJobs.ContainerInstance jobs = new SkiJobs.ContainerInstance(_aciClient) 
                {
                    ContainerImage = _config["SKICONSOLE_IMAGE"],
                    SkiBlobsConnectionString = _config["SKIBLOBS"],
                    GoogleSecret = Base64Encode(_config["GOOGLESKIVIDEOS"]),
                    RegistryResourceGroup = _config["REGISTRY_RESOURCE_GROUP"],
                    RegistryName = _config["REGISTRY_NAME"],
                    JobResourceGroup = _jobsResourceGroupName,
                    CpuCoreCount = _config.GetValue<double>("ACI_CPU", 1.0),
                    MemoryInGb = _config.GetValue<double>("ACI_MEMORY", 3.0)
                };            
                string containerGroup = jobs.Create(videoUrl);

                _logger.LogInformation(
                    $"Created container instance {containerGroup} in {_jobsResourceGroupName} for video: {videoUrl}");
                
                string json = JsonSerializer.Serialize(
                    new {ContainerGroup=containerGroup,VideoUrl=videoUrl});
                
                return StatusCode(200, json);
            }
            catch (CloudException cloudException)
            {
                //
                // Write out everything we can find...
                //
                _logger.LogError(cloudException, $"Unable to create ACI instance for {videoUrl}");
                _logger.LogError("REASON PHRASE: " + cloudException.Response.ReasonPhrase);
                
                foreach (var value in cloudException.Data.Values)
                    _logger.LogError("DATA::VALUES: " + value.ToString());
                
                _logger.LogError("CONTENT: " + cloudException.Response.Content);
                
                foreach (var detail in cloudException.Body.Details)
                    _logger.LogError("DETAIL: " + detail.Message + "\n" + detail.Details); 

                _logger.LogError("BODY::Message: " + cloudException.Body.Message);

                foreach (var info in cloudException.Body.AdditionalInfo)
                    _logger.LogError("BODY::AdditionalInfo: " + info.Info.ToString());

                if (cloudException.InnerException != null)
                    _logger.LogError(cloudException.InnerException, "INNER EXCEPTION");

                return StatusCode(500, cloudException.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unable to create ACI instance for {videoUrl}");
                
                if (e.InnerException != null)
                    _logger.LogError(e.InnerException, $"Inner exception: {e.InnerException.Message}");

                return StatusCode(500, e.Message);
            }

            // inline utility method just for simplicity/readability above.
            static string Base64Encode(string plainText) {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return System.Convert.ToBase64String(plainTextBytes);
            }            
        }

        [HttpPost]
        [Route("deleteall")]
        public int DeleteAll()
        {
            try 
            {
                SkiJobs.ContainerInstance jobs = new SkiJobs.ContainerInstance(_aciClient);

                int count = jobs.DeleteAllContainerGroups(_jobsResourceGroupName);
                var result = new {deletedCount=count};

                return count;
            }
            catch (Exception e)
            {
                string message = $"Error deleting ACI container groups: \n{e.Message}";
                throw new ApplicationException(message);
            }            
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
