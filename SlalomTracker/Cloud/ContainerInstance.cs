using System;
using System.Collections.Generic;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;

namespace SlalomTracker.Cloud
{
    public class ContainerInstance
    {
        const string ExePath = "./ski";
        const string JobResourceGroup = "ski-jobs";

        const string JobNamePrefix = "aci-";

        const string ContainerImageEnvVar = "SKICONSOLE_IMAGE";
        const string RegistryResourceGroup = "ski";
        const string RegistryName = "jasondelAcr";

        const double CpuCoreCount = 1.0;
        const double MemoryInGb = 1.0;

        public static void Create(string videoUrl)
        {
            Console.WriteLine($"Creating container instance for video: {videoUrl}");
            string image = GetContainerImage();
            string[] args = GetCommandLineArgs(videoUrl);
            string containerGroup = GetContainerGroupName(videoUrl);
            Dictionary<string, string> envVars = GetEnvironmentVariables();
            Create(containerGroup, JobResourceGroup, image, ExePath, args, envVars);
            Console.WriteLine($"Created container instance {containerGroup} in {JobResourceGroup} for video: {videoUrl}");
        }

        private static void Create(string containerGroupName, 
            string resourceGroupName, 
            string containerImage,
            string commandLineExe,
            string[] commandLineArgs,
            Dictionary<string, string> environmentVariables)
        {
            var msi = new MSILoginInformation(MSIResourceType.AppService); 
            var credentials = new AzureCredentials(msi, AzureEnvironment.AzureGlobalCloud);
            var authenticated = Azure.Authenticate(credentials);
            string subscriptionId = GetDefaultSubscription(authenticated);
            IAzure azure = authenticated.WithSubscription(subscriptionId);

            // Get private registry credentials.
            var acr = azure.ContainerRegistries.GetByResourceGroup(RegistryResourceGroup, RegistryName);
            var acrCredentials = acr.GetCredentials();
            
            IResourceGroup group = azure.ResourceGroups.GetByName(resourceGroupName);
            var azureRegion = group.Region;
            azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithLinux()
                .WithPrivateImageRegistry(acr.LoginServerUrl, 
                    acrCredentials.Username, 
                    acrCredentials.AccessKeys[AccessKeyType.Primary])
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName + "-0")
                    .WithImage(containerImage)
                    .WithoutPorts()
                    .WithCpuCoreCount(CpuCoreCount)
                    .WithMemorySizeInGB(MemoryInGb)
                    .WithStartingCommandLine(commandLineExe, commandLineArgs)
                    .WithEnvironmentVariables(environmentVariables)
                    .Attach()
                .WithRestartPolicy(ContainerGroupRestartPolicy.Never)
                .CreateAsync();
        }

        private static string GetDefaultSubscription(Azure.IAuthenticated azure)
        {
            string subscriptionId = System.Environment.GetEnvironmentVariable("SUBSCRIPTIONID");
            if (subscriptionId == null)
                throw new ApplicationException("Unable to find a default subscription from SUBSCRIPTIONID env variable");
            
            return subscriptionId;
        }

        private static string[] GetCommandLineArgs(string videoUrl)
        {
            string[] args = new string[] {"-p", videoUrl};
            return args;
        }

        private static string GetContainerGroupName(string videoUrl)
        {
            string unique = GetHash(videoUrl + System.DateTime.Now.Millisecond);
            Console.WriteLine($"Using container group suffix: {unique} for {videoUrl}");
            return (JobNamePrefix + unique).ToLower(); // upper case chars not allowed in ACI naming.
        }

        private static string GetContainerImage()
        {
            string env = Environment.GetEnvironmentVariable(ContainerImageEnvVar);
            if (env == null || env == string.Empty)
                throw new ApplicationException(
                    $"No environment variable set for container image: {ContainerImageEnvVar}");

            return env;
        }        

        private static Dictionary<string, string> GetEnvironmentVariables()
        {
            return new Dictionary<string, string>
            {
                { 
                    Storage.ENV_SKIBLOBS, 
                    Environment.GetEnvironmentVariable(Storage.ENV_SKIBLOBS) 
                }
            };
        }

        private static string GetHash(string value)
        {
            int hash = value.GetHashCode();
            string computed = string.Format("{0:X8}", hash);
            return computed;
        }
    }
}