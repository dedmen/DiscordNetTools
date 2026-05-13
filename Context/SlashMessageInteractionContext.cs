using Discord;

namespace DiscordNetTools.Context
{
    public class SlashMessageInteractionContext : Discord.Interactions.InteractionContext
    {
        public string InteractionContextToken { get; set; } = null;
        public IServiceProvider ServiceProvider { get; set; }

        public SlashMessageInteractionContext(IDiscordClient client, IDiscordInteraction interaction, IMessageChannel channel = null, IServiceProvider serviceProvider = null) : base(client, interaction, channel)
        {
            ServiceProvider = serviceProvider;
        }
    }
}
