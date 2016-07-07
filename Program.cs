using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;

// Azure Management dependencies
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var secret = Environment.GetEnvironmentVariable("AZURE_SECRET");
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            if(new List<string>{ tenantId, clientId, secret, subscriptionId }.Any(i => String.IsNullOrEmpty(i))) {
                Console.WriteLine("Please provide ENV vars for AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_SECRET and AZURE_SUBSCRIPTION_ID.");
            }
            else
            {
                RunSample(tenantId, clientId, secret, subscriptionId).Wait();                
            }
        }

        public static async Task RunSample(string tenantId, string clientId, string secret, string subscriptionId)
        {
            // Build the service credentials and Azure Resource Manager clients
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret);
            var resourceClient = new ResourceManagementClient(serviceCreds);
            resourceClient.SubscriptionId = subscriptionId;

            Random r = new Random();
            int postfix = r.Next(0, 1000000);

            var resourceGroupName = "sample-dotnet-group-mgmt";
            var westus = "westus";

            Write("Listing resource groups:");
            resourceClient.ResourceGroups.List().ToList().ForEach(rg => {
                Write("\tName: {0}, Id: {1}", rg.Name, rg.Id);
            });
            Write(Environment.NewLine);

            Write("Creating resource group named {0} in {1}", resourceGroupName, westus);
            var groupParams = new ResourceGroup { Location = westus};
            resourceClient.ResourceGroups.CreateOrUpdate(resourceGroupName, groupParams);
            Write(Environment.NewLine);
            
            Write("Adding tags to the resource group");
            groupParams.Tags = new Dictionary<string, string>{{"Hello", "World"}};
            resourceClient.ResourceGroups.CreateOrUpdate(resourceGroupName, groupParams);
            Write(Environment.NewLine);

            Write("Listing resource groups:");
            resourceClient.ResourceGroups.List().ToList().ForEach(rg => {
                Write("\tName: {0}, Id: {1}", rg.Name, rg.Id);
            });
            Write(Environment.NewLine);

            Write("Create a Key Vault resource with a generic PUT");
            var keyVaultParams = new GenericResource{
                Location = westus,
                Properties = new Dictionary<string, object>{
                    {"tenantId", tenantId},
                    {"sku", new Dictionary<string, object>{{"family", "A"}, {"name", "standard"}}},
                    {"accessPolicies", Array.Empty<string>()},
                    {"enabledForDeployment", true},
                    {"enabledForTemplateDeployment", true},
                    {"enabledForDiskEncryption", true}
                }
            };
            var keyVault = resourceClient.Resources.CreateOrUpdate(
                resourceGroupName,
                "Microsoft.KeyVault",
                "",
                "vaults",
                "azureSampleVault",
                "2015-06-01",
                keyVaultParams);
            Write("\tKey Vault Name: {0} and Id: {1}", keyVault.Name, keyVault.Id);
            Write(Environment.NewLine);

            Write("Listing resources within group {0}", resourceGroupName);
            resourceClient.ResourceGroups.ListResources(resourceGroupName).ToList().ForEach(resource => {
                Write("\tName: {0}, Id: {1}", resource.Name, resource.Id);
            });
            Write(Environment.NewLine);

            Write("Exporting the resource group template for {0}", resourceGroupName);
            Write(Environment.NewLine);
            var exportResult = resourceClient.ResourceGroups.ExportTemplate(
                resourceGroupName, 
                new ExportTemplateRequest{ 
                    Resources = new List<string>{"*"}
                });
            Write("{0}", exportResult.Template);
            Write(Environment.NewLine);

            Write("Press any key to continue and delete the sample resources");
            Console.ReadLine();
            Write(Environment.NewLine);

            Write("deleting resource group {0} and all resources within it", resourceGroupName);
            resourceClient.ResourceGroups.Delete(resourceGroupName);
        }

        private static void Write(string format, params object[] items) 
        {
            Console.WriteLine(String.Format(format, items));
        }
    }
}
