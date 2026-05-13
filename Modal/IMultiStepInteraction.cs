using Discord;
using Discord.Interactions;
using DiscordNetTools.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InteractionContext = DiscordNetTools.Context.InteractionContext;

namespace DiscordNetTools.Modal
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MultiStepInteractionAssociationAttribute : Attribute
    {
        // Create a field to store the specified name
        public readonly Type MultiModalType;

        // Create a constructor so the name can be specified
        public MultiStepInteractionAssociationAttribute(Type name) => MultiModalType = name;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class MultiStepComponentInteractionAssociationAttribute : Attribute
    {
        // Create a field to store the specified name
        public readonly string ComponentInteractionId;

        // Create a constructor so the name can be specified
        public MultiStepComponentInteractionAssociationAttribute(string name) => ComponentInteractionId = name;
    }

    public interface IMultiStepInteraction
    {
        public static Task FirstStep(IInteractionContext context) => throw new NotImplementedException();
        // If previous step was a IModalWithValidation, then we pass its validationResult which may contain further information
        public static Task NextStep(IInteractionContext context, ValidationResult validationResult, IModal interactionModal = null) => throw new NotImplementedException();

        static async Task NextModal(IInteractionContext context, Type modalType)
        {
            //var modalAttribute = (DiscordModalAttribute) Attribute.GetCustomAttribute(modalType, typeof(DiscordModalAttribute));

            var builder = new ComponentBuilder()
                .WithButton("Next Step", InteractionContext.Get(context).WrapInteraction($"_openModal_{modalType.Name}"));
            await context.Interaction.RespondAsync("Everything was correct, but we're not done yet, please proceed to the next step.", components: builder.Build(), ephemeral: true);
        }


        // This cannot be used in modal submit handlers, but it can be used on buttons
        static async Task OpenModalDirect(IInteractionContext context, Type modalType, bool keepOriginalInteraction = false)
        {

            var userData = await ModalToolsCommandModule.ModalDataLookupFunction?.Invoke(context, modalType.Name);

            var modal = ModalToolsCommandModule.CreateModal(modalType, userData);
            await context.Interaction.RespondWithModalAsync(InteractionContext.Get(context).WrapInteraction(modal));

            if (!keepOriginalInteraction && context.Interaction is IComponentInteraction discordInteraction)
            {
                //await discordInteraction.DeleteOriginalResponseAsync(); // On evening of 5th august 2024, it suddenly stopped working. If we delete it, the next modal cannot submit.
                await discordInteraction.ModifyOriginalResponseAsync(x => x.Components = new ComponentBuilder().Build());
            }
        }
    }

    public interface IMultiStepInteractionValidationErrorHandler
    {
        enum ValidationErrorHandlerContinuation
        {
            // Default, present validationResult.Message to the user
            PresentToUser,
            // Do not tell the user, and do not abort, NextStep() will be called to continue
            IgnoreValidationFailure,
            // Abort the modal handling, usually because we already handled the response
            AbortHandling
        }

        // Returns true if the uesr can bypass the validation error
        public static Task<ValidationErrorHandlerContinuation> OnValidationError(IInteractionContext context, ValidationResult validationResult, IModal interactionModal = null) => throw new NotImplementedException();
    }
}
