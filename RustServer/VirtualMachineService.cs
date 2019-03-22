using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Threading.Tasks;

namespace RustServer
{
    public class VirtualMachineService
    {
        private readonly string resourceGroup;
        private readonly string vmName;

        public VirtualMachineService(string resourceGroup, string vmName)
        {
            this.resourceGroup = resourceGroup;
            this.vmName = vmName;
        }

        public async Task<IVirtualMachine> GetVirtualMachineAsync()
        {
            AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromMSI(new MSILoginInformation(MSIResourceType.AppService), AzureEnvironment.AzureGlobalCloud);

            IAzure azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();

            return await azure.VirtualMachines.GetByResourceGroupAsync(resourceGroup, vmName);
        }
        public Task StartVirtualMachineAsync(IVirtualMachine vm)
        {
            return vm.StartAsync();
        }

        public Task StopVirtualMachineAsync(IVirtualMachine vm)
        {
            return vm.DeallocateAsync();
        }
    }
}
