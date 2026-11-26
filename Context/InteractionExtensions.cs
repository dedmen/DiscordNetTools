using Discord;
using Discord.Interactions;
using System.Threading.Tasks;

namespace DiscordNetTools.Context
{
    public static class InteractionExtensions
    {
        /// <summary>
        /// RespondAsync while the interaction is still unanswered, FollowupAsync once it was already
        /// acknowledged (see IModalWithValidation.AckOnSubmit) — the interaction cannot be responded to twice.
        /// Note: RespondAsync doesn't return the message in Discord.Net 3.20.2, so this returns null on that path.
        /// </summary>
        public static async Task<IUserMessage?> RespondOrFollowUpAsync(this IInteractionContext context, string text, MessageComponent? components = null, bool ephemeral = true)
        {
            var state = InteractionContext.Get(context).GetOrCreate<InteractionState>();
            if (!state.Acknowledged)
            {
                await context.Interaction.RespondAsync(text, components: components, ephemeral: ephemeral);
                return null;
            }

            return await context.Interaction.FollowupAsync(text, components: components, ephemeral: ephemeral);
        }
    }
}
