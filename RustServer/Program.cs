using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace RustServer
{
    public class Program
    {
        private static DiscordClient discord;
        private static VirtualMachineService virtualMachineService;
        private static Random random = new Random();

        public static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                 .AddEnvironmentVariables()
                 .Build();

            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = configuration["DiscordToken"],
                TokenType = TokenType.Bot
            });

            virtualMachineService = new VirtualMachineService(configuration["VmResourceGroup"], configuration["VmName"]);
            discord.MessageCreated += Discord_MessageCreated;

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task Discord_MessageCreated(MessageCreateEventArgs e)
        {
            try
            {
                if (e.Message.Content.ToLower().StartsWith("#!server start", StringComparison.InvariantCultureIgnoreCase))
                {
                    var azure = virtualMachineService.GetAzure();
                    IVirtualMachine vm = await virtualMachineService.GetVirtualMachineAsync(azure);
                    if (vm.PowerState == PowerState.Running)
                    {
                        await e.Message.RespondAsync("Rust server is already running! It takes a few minutes before rust server is connectable.");
                        return;
                    }

                    if (vm.PowerState == PowerState.Starting)
                    {
                        await e.Message.RespondAsync("Rust server is already starting!");
                        return;
                    }

                    Task startTask = virtualMachineService.StartVirtualMachineAsync(vm);
                    await e.Message.RespondAsync("Starting Rust Server!");
                    await startTask;
                    await e.Message.RespondAsync("Rust Server started, ETA 5 min until you can connect! Hell yeah bizzes.");
                    return;
                }

                if (e.Message.Content.StartsWith("#!server stop", StringComparison.InvariantCultureIgnoreCase))
                {
                    var azure = virtualMachineService.GetAzure();
                    IVirtualMachine vm = await virtualMachineService.GetVirtualMachineAsync(azure);
                    if (vm.PowerState == PowerState.Deallocated || vm.PowerState == PowerState.Deallocating || vm.PowerState == PowerState.Stopped)
                    {
                        await e.Message.RespondAsync("Rust server is stopped!");
                        return;
                    }

                    if (vm.PowerState == PowerState.Stopping)
                    {
                        await e.Message.RespondAsync("Rust server is already stopping!");
                        return;
                    }

                    Task stopTask = virtualMachineService.StopVirtualMachineAsync(vm);
                    await e.Message.RespondAsync("Stopping Rust Server!");
                    await stopTask;
                    await e.Message.RespondAsync("Rust Server stopped, goodnight :(");
                    return;
                }

                if (e.Message.Content.StartsWith("#!ping", StringComparison.InvariantCultureIgnoreCase))
                {
                    await e.Message.RespondAsync("Pong");
                    return;
                }
            }
            catch (Exception ex)
            {
                await e.Message.RespondAsync(ex.Message);
            }
        }
    }
}
