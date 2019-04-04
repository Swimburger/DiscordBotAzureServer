using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RustServer
{
    public class Program
    {
        private const string RustDiscordAdminRole = "rust-admin";
        private static DiscordClient discord;
        private static VirtualMachineService virtualMachineService;

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
                if (e.Author.IsBot)
                {
                    return;
                }

                if (e.Guild == null)
                {
                    await e.Message.RespondAsync("Bot not allowed outside of Guild");
                    return;
                }

                if (e.Message.Content.StartsWith("#!server init discord roles", StringComparison.InvariantCultureIgnoreCase))
                {
                    await InitDiscordRustRole(e);
                    return;
                }

                if (e.Message.Content.StartsWith("#!server start", StringComparison.InvariantCultureIgnoreCase))
                {
                    await StartServer(e);
                    return;
                }

                if (e.Message.Content.StartsWith("#!server stop", StringComparison.InvariantCultureIgnoreCase))
                {
                    await StopServer(e);
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

        private static async Task InitDiscordRustRole(MessageCreateEventArgs e)
        {
            var role = e.Guild.Roles.SingleOrDefault(r => r.Name == RustDiscordAdminRole);
            if (role == null)
            {
                await e.Guild.CreateRoleAsync(RustDiscordAdminRole, reason: "Role for managing rust server via Discord");
                await e.Message.RespondAsync($"Role created: {RustDiscordAdminRole}");
            }
        }

        private static async Task StartServer(MessageCreateEventArgs e)
        {
            var member = e.Author as DiscordMember;
            if(!member.Roles.Any(r => r.Name == RustDiscordAdminRole))
            {
                await e.Message.RespondAsync($"{member.DisplayName} does not have '{RustDiscordAdminRole}' role and is not allowed to perform this action.");
                return;
            }

            var azure = virtualMachineService.GetAzure();
            var vm = await virtualMachineService.GetVirtualMachineAsync(azure);
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

            var startTask = virtualMachineService.StartVirtualMachineAsync(vm);
            await e.Message.RespondAsync("Starting Rust Server!");
            await startTask;
            await vm.RunPowerShellScriptAsync(new List<string> { "start \"c:\\RustServer\\Start.bat\" /REALTIME" }, new List<RunCommandInputParameter>());
            await e.Message.RespondAsync("Rust Server started, ETA 5 min until you can connect! Let's get schwifty");
            await e.Message.RespondAsync("Press f1 and type 'client.connect rust.swimburger.net:28015'");
        }

        private async static Task StopServer(MessageCreateEventArgs e)
        {
            var member = e.Author as DiscordMember;
            if (!member.Roles.Any(r => r.Name == RustDiscordAdminRole))
            {
                await e.Message.RespondAsync($"{member.DisplayName} does not have '{RustDiscordAdminRole}' role and is not allowed to perform this action.");
                return;
            }

            var azure = virtualMachineService.GetAzure();
            var vm = await virtualMachineService.GetVirtualMachineAsync(azure);
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

            var stopTask = virtualMachineService.StopVirtualMachineAsync(vm);
            await e.Message.RespondAsync("Stopping Rust Server!");
            await stopTask;
            await e.Message.RespondAsync("Rust Server stopped, goodnight :(");
        }        
    }
}
