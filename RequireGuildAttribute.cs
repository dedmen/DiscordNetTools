using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNetTools
{
    public class RequireGuildAttribute : PreconditionAttribute
    {
        // Create a field to store the specified name
        public readonly ulong[] Guilds;

        // Create a constructor so the name can be specified
        public RequireGuildAttribute(ulong[] guildIds) => Guilds = guildIds;

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            // Check if this user is a Guild User, which is the only context where roles exist
            if (context.User is SocketGuildUser gUser)
            {
                // If this command was executed by a user with the appropriate role, return a success
                if (Guilds.Any(x => x == context.Guild.Id))
                    // Since no async work is done, the result has to be wrapped with `Task.FromResult` to avoid compiler errors
                    return Task.FromResult(PreconditionResult.FromSuccess());
                // Since it wasn't, fail
                return Task.FromResult(PreconditionResult.FromError($"This command can only be executed in guilds: {JsonConvert.SerializeObject(Guilds)}"));
            }

            return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));
        }
    }
}
