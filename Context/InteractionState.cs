namespace DiscordNetTools.Context
{
    /// <summary>
    /// Response state of one interaction, stored in the InteractionContext for the duration of the flow.
    /// </summary>
    public class InteractionState
    {
        // Set when the interaction was already responded to (e.g. via IModalWithValidation.AckOnSubmit).
        // Every later message in the flow must then be sent as a follow-up instead of a response.
        public bool Acknowledged = false;
    }
}
