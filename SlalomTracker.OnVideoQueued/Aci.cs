using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Microsoft.Azure.Management.ContainerInstance.Fluent.ContainerGroup.Definition;

namespace aci
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // Build request to acquire managed identities for Azure resources token
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://management.azure.com/");
            //request.Headers["Metadata"] = "true";
            //request.Method = "GET";

            try
            {
                // Call /token endpoint
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Pipe response Stream to a StreamReader, and extract access token
               //StreamReader streamResponse = new StreamReader(response.GetResponseStream());
                //string stringResponse = streamResponse.ReadToEnd();
                //Console.WriteLine("Access: " + stringResponse);

                string containerGroupName = "aci-msi-test";
                string resourceGroupName = "ubuntu";
                string containerImage = "busybox";
                Dictionary<string, string> envVars = new Dictionary<string, string>();
                //string[] startCommandLine = {"/bin/ash", "-c 'sleep 3600'"};
                string startCommandLine = "/bin/ash -c sleep 3600";

                var msi = new MSILoginInformation(MSIResourceType.VirtualMachine); // try 1
                var credentials = new AzureCredentials(msi, AzureEnvironment.AzureGlobalCloud);
                var authenticated = Azure.Authenticate(credentials);
                if (authenticated == null)
                        throw new Exception("Unable to authenticate.");
                IAzure azure = authenticated.WithDefaultSubscription();

                IResourceGroup group = azure.ResourceGroups.GetByName(resourceGroupName);
                var azureRegion = group.Region;
/*
    var containerGroup = azure.ContainerGroups.Define(containerGroupName)
        .WithRegion(azureRegion)
        .WithExistingResourceGroup(groupName)
        .WithLinux()
        .WithPublicImageRegistryOnly()
        .WithoutVolume()
        .DefineContainerInstance(containerGroupName + "-1")
            .WithImage(containerImage)
            .WithExternalTcpPort(80)
            .WithCpuCoreCount(1.0)
            .WithMemorySizeInGB(1)
            .WithStartingCommandLines(startCommandLine.Split())
            .WithEnvironmentVariables(envVars)
            .Attach()
        .WithDnsPrefix(containerGroupName)
        .WithRestartPolicy(ContainerGroupRestartPolicy.Always)
        .Create();
*/

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
                    //.WithStartingCommandLine(startCommandLine)
                    .WithStartingCommandLine("/bin/ash", new string[] {"-c", "sleep 3600"})
                    .WithEnvironmentVariables(envVars)
                    .Attach()
                //.WithDnsPrefix(containerGroupName)
                .Create();

                //Console.WriteLine($"Logs for container '{containerGroupName}-1':");
                //Console.WriteLine(containerGroup.GetLogContent(containerGroupName + "-1"));
                Console.WriteLine("Created...");


            }
            catch (Exception e)
            {                               
                string errorText = String.Format("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : "Acquire token failed");
                Console.WriteLine("ERROR: " + e);
            }

        }
    }
}