using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNetTools.Modal
{
    public class UserDiscordModalContent
    {
        [Required]
        public UInt64 DiscordUserId { get; set; }
        [Required]
        public string ModalName { get; set; }
        [Required]
        public System.Text.Json.Nodes.JsonObject Data { get; set; } = new();

        public string GetFieldContent(string fieldName)
        {
            return Data.ContainsKey(fieldName) ? Data[fieldName].AsValue().ToString() : null;
        }

        public IEnumerable<T> GetFieldContentArray<T>(string fieldName)
        {
            return Data.ContainsKey(fieldName) ? Data[fieldName].AsArray().Select(x => x.AsValue().GetValue<T>()) : Array.Empty<T>();
        }

        public void SetFieldContent(string fieldName, string content)
        {
            if (!Data.ContainsKey(fieldName))
                Data.Add(fieldName, System.Text.Json.Nodes.JsonValue.Create(content));
            else
                Data[fieldName].ReplaceWith(content);
        }

        public void SetFieldContent(string fieldName, ulong[] content)
        {
            Data[fieldName].ReplaceWith(System.Text.Json.JsonSerializer.SerializeToNode(content));
        }
    }

    // This is used for modal creation, providing info like the select option for a select menu
    public class UserDiscordModalContentWithExtraData : UserDiscordModalContent
    {
        public Dictionary<string, object> ExtraData = new();
    }
}
