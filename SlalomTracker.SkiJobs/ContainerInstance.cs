using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.Management.ContainerInstance;

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
            string[] args = GetCommandLineArgs(videoUrl);
            string containerGroup = GetContainerGroupName(videoUrl);
            Dictionary<string, string> envVars = GetEnvironmentVariables();
            InternalCreate(containerGroup, JobResourceGroup, ContainerImage, ExePath, args, envVars);
            
            return containerGroup;
        }

        private static void InternalCreate(string containerGroupName, 
            string resourceGroupName, 
            string containerImage,
            string commandLineExe,
            string[] commandLineArgs,
            Dictionary<string, string> environmentVariables)
        {
            // IAzure azure = Authenticate();

            // // Get private registry credentials.
            // var acr = azure.ContainerRegistries.GetByResourceGroup(RegistryResourceGroup, RegistryName);
            // var acrCredentials = acr.GetCredentials();
            
            // IResourceGroup group = azure.ResourceGroups.GetByName(resourceGroupName);
            // var azureRegion = group.Region;
            // azure.ContainerGroups.Define(containerGroupName)
            //     .WithRegion(azureRegion)
            //     .WithExistingResourceGroup(resourceGroupName)
            //     .WithLinux()
            //     .WithPrivateImageRegistry(acr.LoginServerUrl, 
            //         acrCredentials.Username, 
            //         acrCredentials.AccessKeys[AccessKeyType.Primary])
            //     .WithoutVolume()
            //     .DefineContainerInstance(containerGroupName + "-0")
            //         .WithImage(containerImage)
            //         .WithoutPorts()
            //         .WithCpuCoreCount(CpuCoreCount)
            //         .WithMemorySizeInGB(MemoryInGb)
            //         .WithStartingCommandLine(commandLineExe, commandLineArgs)
            //         .WithEnvironmentVariables(environmentVariables)
            //         .Attach()
            //     .WithRestartPolicy(ContainerGroupRestartPolicy.Never)
            //     .CreateAsync();
        }

        private string[] GetCommandLineArgs(string videoUrl)
        {
            string[] args = new string[] {"-p", videoUrl};
            return args;
        }

        private string GetContainerGroupName(string videoUrl)
        {
            string unique = GetHash(videoUrl + System.DateTime.Now.Millisecond);
            Console.WriteLine($"Using container group suffix: {unique} for {videoUrl}");
            return (JobNamePrefix + unique).ToLower(); // upper case chars not allowed in ACI naming.
        }

        private Dictionary<string, string> GetEnvironmentVariables()
        {
            return new Dictionary<string, string>
            {
                { 
                    ENV_SKIBLOBS, 
                    SkiBlobsConnectionString
                }
            };
        }

        private string GetHash(string value)
        {
            int hash = value.GetHashCode();
            string computed = string.Format("{0:X8}", hash);
            return computed;
        }
    }
}