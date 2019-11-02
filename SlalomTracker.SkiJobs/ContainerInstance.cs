using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.ContainerInstance;
using Microsoft.Azure.Management.ContainerInstance.Models;


namespace SlalomTracker.SkiJobs
{
    public class ContainerInstance
    {
        const string ExePath = "./ski";
        const string JobNamePrefix = "aci-";
        const string ENV_SKIBLOBS = "SKIBLOBS";

        public string JobResourceGroup;
        public string ContainerImage;
        public string RegistryResourceGroup;
        public string RegistryName; 
        public double CpuCoreCount;
        public double MemoryInGb;
        public string SkiBlobsConnectionString { private get; set; }

        private readonly ContainerInstanceManagementClient _aciClient;

        public ContainerInstance(ContainerInstanceManagementClient aciClient)
        {
            _aciClient = aciClient;
        }

        public int DeleteAllContainerGroups(string jobResourceGroup)
        {
            var containerGroups = _aciClient.ContainerGroups.ListByResourceGroup(jobResourceGroup);
            int count = containerGroups.Count();

            foreach (var containerGroup in containerGroups)
                _aciClient.ContainerGroups.Delete(jobResourceGroup, containerGroup.Name);
        
            return count;
        }

        public string Create(string videoUrl)
        {
            string name = GetContainerGroupName(videoUrl);
            ContainerGroup group = GetContainerGroup(videoUrl);
            group.Containers[0].Name = name + "-0";
            _aciClient.ContainerGroups.CreateOrUpdate(JobResourceGroup, name, group);
            
            return name;
        }

        private ContainerGroup GetContainerGroup(string videoUrl)
        {
            ContainerGroup group = new ContainerGroup();
            group.Location = GetLocation();
            group.OsType = "Linux";
            group.ImageRegistryCredentials = GetAcrCredentials();
            group.Containers = new List<Microsoft.Azure.Management.ContainerInstance.Models.Container>();
            group.Containers.Add(GetContainer(videoUrl));
            group.RestartPolicy = "Never";

            return group;
        }

        private string GetLocation()
        {
            // Need to get this from the ResourceGroup
            #warning "Get location from RG"
            return "eastus";
        }

        private IList<ImageRegistryCredential> GetAcrCredentials()
        {
            ContainerRegistryManagementClient acrClient = new ContainerRegistryManagementClient(_aciClient.Credentials);
            acrClient.SubscriptionId = _aciClient.SubscriptionId;
            
            IList<ImageRegistryCredential> credentials = new List<ImageRegistryCredential>();

            var registry = acrClient.Registries.Get(RegistryResourceGroup, RegistryName);

            var acrCredentials = acrClient.Registries.ListCredentials(RegistryResourceGroup, RegistryName);
            if (acrCredentials.Passwords != null && acrCredentials.Passwords.Count > 0)
            {
                credentials.Add(new ImageRegistryCredential(
                    registry.LoginServer, acrCredentials.Username, acrCredentials.Passwords[0].Value));
            }

            return credentials;
        }

        private Container GetContainer(string videoUrl)
        {
            Container container = new Container();
            container.Image = ContainerImage;
            container.Resources = GetResourceRequirements();
            container.Command = GetCommandLineArgs(videoUrl);
            container.EnvironmentVariables = GetEnvironmentVariables();

            return container;
        }

        private ResourceRequirements GetResourceRequirements()
        {
            ResourceRequirements resources = new ResourceRequirements();
            resources.Limits = new ResourceLimits(MemoryInGb, CpuCoreCount);
            resources.Requests = new ResourceRequests(MemoryInGb, CpuCoreCount);
            return resources;
        }

        private IList<string> GetCommandLineArgs(string videoUrl)
        {
            //string[] commands = { ExePath, "-p", videoUrl };
            string[] commands = { ExePath, "-m" };
            return commands.ToList();
        }

        private string GetContainerGroupName(string videoUrl)
        {
            string unique = GetHash(videoUrl + System.DateTime.Now.Millisecond);
            Console.WriteLine($"Using container group suffix: {unique} for {videoUrl}");
            return (JobNamePrefix + unique).ToLower(); // upper case chars not allowed in ACI naming.
        }

        private IList<EnvironmentVariable> GetEnvironmentVariables()
        {
            IList<EnvironmentVariable> env = new List<EnvironmentVariable>();
            env.Add(new EnvironmentVariable(ENV_SKIBLOBS, null, SkiBlobsConnectionString));

            return env;
        }

        private string GetHash(string value)
        {
            int hash = value.GetHashCode();
            string computed = string.Format("{0:X8}", hash);
            return computed;
        }
    }
}