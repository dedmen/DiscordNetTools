using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNetTools.Modal
{
    //[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    //public class DiscordModalAttribute : Attribute
    //{
    //    // Create a field to store the specified name
    //    public readonly string Name;
    //
    //    // Create a constructor so the name can be specified
    //    public DiscordModalAttribute(string name) => Name = name;
    //}

    public class ValidationResult
    {
        public bool Success;
        //public bool CanRetry;
        public string Message;
    }

    // Extra information, for something. Is passed to modal's NextStep() member
    public class ValidationResultExtra : ValidationResult
    {
        public string ExtraData;
    }

    public interface IModalWithValidation : IModal
    {
        public bool SaveToDb => true;

        Task<ValidationResult> Validate(IInteractionContext context);

        public static ValidationResult Fail(string message, bool canRetry = true)
        {
            return new ValidationResult { Success = false, Message = message/*, CanRetry = canRetry*/ };
        }

        public static ValidationResult Success()
        {
            return new ValidationResult { Success = true };
        }
    }
}
