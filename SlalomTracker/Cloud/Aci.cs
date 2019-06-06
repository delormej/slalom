using System;
using System.IO;
using System.Net;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Microsoft.Azure.Management.ContainerInstance.Fluent.ContainerGroup.Definition;

namespace SlalomTracker.Cloud
{
    public class Aci
    {
        static void Main(string[] args)
        {
            try
            {
                string containerGroupName = "aci-msi-test";
                string resourceGroupName = "ubuntu";
                string containerImage = "busybox";
                string commandLineExe = "/bin/ash";
                string[] commandLineArgs = new string[] {"-c", "sleep 3600"};
                CreateContainerInstance(containerGroupName, 
                    resourceGroupName, 
                    containerImage,
                    commandLineExe,
                    commandLineArgs);
            }
            catch (Exception e)
            {                               
                string errorText = String.Format("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : "Unable to create container instance.");
                Console.WriteLine("ERROR: " + e);
            }
        }

        public static void CreateContainerInstance(string containerGroupName, 
            string resourceGroupName, 
            string containerImage,
            string commandLineExe,
            string[] commandLineArgs)
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
                .DefineContainerInstance(containerGroupName + "-1")
                    .WithImage(containerImage)
                    .WithoutPorts()
                    .WithCpuCoreCount(1.0)
                    .WithMemorySizeInGB(1)
                    .WithStartingCommandLine(commandLineExe, commandLineArgs)
                    .Attach()
                .Create();
        }
    }
}