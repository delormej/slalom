using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Microsoft.Azure.Management.ContainerInstance.Fluent.ContainerGroup.Definition;

namespace SlalomTracker.Cloud
{
    public class ContainerInstance
    {
        const string ExePath = "/ski/ski";
        const string ResourceGroup = "ski-jobs";

        const string JobNamePrefix = "aciski-";

        const string ContainerImageEnvVar = "SKICONSOLE_IMAGE";

        const double CpuCoreCount = 1.0;
        const double MemoryInGb = 1.0;

        public static void Create(string videoUrl)
        {
            string image = GetContainerImage();
            string[] args = GetCommandLineArgs(videoUrl);
            string containerGroup = GetContainerGroupName(videoUrl);
            Dictionary<string, string> envVars = GetEnvironmentVariables();
            Create(containerGroup, ResourceGroup, image, ExePath, args, envVars);
        }

        private static void Create(string containerGroupName, 
            string resourceGroupName, 
            string containerImage,
            string commandLineExe,
            string[] commandLineArgs,
            Dictionary<string, string> environmentVariables)
        {
            var msi = new MSILoginInformation(MSIResourceType.VirtualMachine); 
            var credentials = new AzureCredentials(msi, AzureEnvironment.AzureGlobalCloud);
            var authenticated = Azure.Authenticate(credentials);
            IAzure azure = authenticated.WithDefaultSubscription();

            IResourceGroup group = azure.ResourceGroups.GetByName(resourceGroupName);
            var azureRegion = group.Region;
            var containerGroup = azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithLinux()
                .WithPublicImageRegistryOnly()
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName)
                    .WithImage(containerImage)
                    .WithoutPorts()
                    .WithCpuCoreCount(CpuCoreCount)
                    .WithMemorySizeInGB(MemoryInGb)
                    .WithStartingCommandLine(commandLineExe, commandLineArgs)
                    .WithEnvironmentVariables(environmentVariables)
                    .Attach()
                .Create();
        }

        private static string[] GetCommandLineArgs(string videoUrl)
        {
            string[] args = new string[] {"-p", videoUrl};
            return args;
        }

        private static string GetContainerGroupName(string videoUrl)
        {
            string unique = GetHash(videoUrl + System.DateTime.Now.Millisecond);
            return JobNamePrefix + unique;
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

        private static string GetHash(string videoUrl)
        {
            MD5 hash = MD5.Create();
            byte[] data = hash.ComputeHash(Encoding.UTF8.GetBytes(videoUrl));
            string computed = Encoding.UTF8.GetString(data);
            return computed;
        }
    }
}