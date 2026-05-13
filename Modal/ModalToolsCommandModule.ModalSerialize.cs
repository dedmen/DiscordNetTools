using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DiscordNetTools.Modal
{
    public partial class ModalToolsCommandModule
    {
        private static bool IsValidModalInputDefinition(PropertyInfo propertyInfo)
        {
            return propertyInfo.SetMethod?.IsPublic == true &&
                   propertyInfo.SetMethod?.IsStatic == false &&
                   propertyInfo.IsDefined(typeof(ModalInputAttribute));
        }

        private static void BuildTextInput(ModalBuilder modalBuilder, PropertyInfo propertyInfo, UserDiscordModalContent? userDiscordModalContent)
        {
            string label = propertyInfo.Name;
            string customId = null;
            TextInputStyle style = TextInputStyle.Short;
            string placeholder = null;
            int? minLength = null;
            int? maxLength = null;
            bool? required = null;
            string value = null;
            int? id = null;
            string description = null;
            int? labelId = null;

            foreach (var attribute in propertyInfo.GetCustomAttributes())
            {
                switch (attribute)
                {
                    case ModalTextInputAttribute textInput:
                        customId = textInput.CustomId;
                        style = textInput.Style;
                        placeholder = textInput.Placeholder;
                        minLength = textInput.MinLength;
                        maxLength = textInput.MaxLength;
                        id = textInput.Id;

                        var fieldName = propertyInfo.GetCustomAttribute(typeof(JsonPropertyNameAttribute)) is JsonPropertyNameAttribute jsonNameAttrib ? jsonNameAttrib.Name : propertyInfo.Name;

                        var fieldValue = userDiscordModalContent?.GetFieldContent(propertyInfo.Name) ?? textInput.InitialValue;

                        value = string.IsNullOrEmpty(fieldValue) ? null : fieldValue; // We cannot assign empty string, needs to be null if unset
                        break;
                    case RequiredInputAttribute requiredInput:
                        required = requiredInput.IsRequired;
                        break;
                    case InputLabelAttribute inputLabel:
                        label = inputLabel.Label;
                        description = inputLabel.Description;
                        break;
                    default:
                        break;
                }
            }

            modalBuilder.AddTextInput(label, customId, style, placeholder, minLength, maxLength, required, value, id, description, labelId);
        }

        private static void BuildFileUpload(ModalBuilder modalBuilder, PropertyInfo propertyInfo, UserDiscordModalContent? userDiscordModalContent)
        {
            var attributes = propertyInfo.GetCustomAttributes();

            string label = propertyInfo.Name;
            string customId = null;
            int? minValues = null;
            int? maxValues = null;
            bool isRequired = true;
            int? id = null;
            string description = null;
            int? labelId = null;

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case ModalFileUploadAttribute textInput:
                        customId = textInput.CustomId;
                        minValues = textInput.MinValues;
                        maxValues = textInput.MaxValues;
                        id = textInput.Id;

                        // File upload cannot have a default value

                        break;
                    case RequiredInputAttribute requiredInput:
                        isRequired = requiredInput.IsRequired;
                        break;
                    case InputLabelAttribute inputLabel:
                        label = inputLabel.Label;
                        description = inputLabel.Description;
                        break;
                    default:
                        break;
                }
            }

            modalBuilder.AddFileUpload(label, customId, minValues, maxValues, isRequired, id, description, labelId);
        }

        private static void BuildRoleSelect(ModalBuilder modalBuilder, PropertyInfo propertyInfo, UserDiscordModalContent? userDiscordModalContent)
        {
            var attributes = propertyInfo.GetCustomAttributes();

            string label = propertyInfo.Name;
            string customId = null;
            List<SelectMenuOptionBuilder> options = null;
            string placeholder = null;
            int minValues = 1;
            int maxValues = 1;
            bool isRequired = true;
            bool disabled = false;
            ComponentType type = ComponentType.RoleSelect; // This
            ChannelType[] channelTypes = null;
            SelectMenuDefaultValue[] defaultValues = null;
            int? id = null;
            string description = null;
            int? labelId = null;

            var builder = new SelectMenuBuilder();

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case ModalRoleSelectAttribute textInput:

                        customId = textInput.CustomId;
                        placeholder = textInput.Placeholder;
                        minValues = textInput.MinValues;
                        maxValues = textInput.MaxValues;
                        id = textInput.Id;

                        var fieldName = propertyInfo.GetCustomAttribute(typeof(JsonPropertyNameAttribute)) is JsonPropertyNameAttribute jsonNameAttrib ? jsonNameAttrib.Name : propertyInfo.Name;

                        // Role IDs
                        var fieldValue = userDiscordModalContent?.GetFieldContentArray<ulong>(propertyInfo.Name) ?? Array.Empty<ulong>();

                        defaultValues = fieldValue.Select(x => new SelectMenuDefaultValue(x, SelectDefaultValueType.Role)).ToArray();

                        //defaultValues = string.IsNullOrEmpty(fieldValue) ? null : fieldValue; // We cannot assign empty string, needs to be null if unset

                        break;
                    case RequiredInputAttribute requiredInput:
                        isRequired = requiredInput.IsRequired;
                        break;
                    case InputLabelAttribute inputLabel:
                        label = inputLabel.Label;
                        description = inputLabel.Description;
                        break;
                    default:
                        break;
                }
            }

            modalBuilder.AddSelectMenu(label, customId, null, placeholder, minValues, maxValues, isRequired, disabled, type, null, defaultValues, id, description, labelId);
        }


        public static Discord.Modal CreateModalFromName(string modalName, UserDiscordModalContent? preFillContent)
        {
            Type modalType = ModalDirectory.Instance.GetModalType(modalName);
            return CreateModal(modalType, preFillContent);
        }

        public static Discord.Modal CreateModal(Type modalType, UserDiscordModalContent? preFillContent)
        {
            var instance = Activator.CreateInstance(modalType, false) as IModal;
            //var modalAttribute = (DiscordModalAttribute)Attribute.GetCustomAttribute(modalType, typeof(DiscordModalAttribute));

            var builder = new ModalBuilder(instance.Title, modalType.Name)
            {
                Title = instance.Title
            };

            var inputs = modalType.GetProperties().Where(IsValidModalInputDefinition);

            foreach (var prop in inputs)
            {
                var attrib = prop.GetCustomAttribute<ModalInputAttribute>();
                var componentType = attrib?.ComponentType;

                switch (componentType)
                {
                    case ComponentType.TextInput:
                        BuildTextInput(builder, prop, preFillContent);
                        break;

                    case ComponentType.FileUpload:
                        BuildFileUpload(builder, prop, preFillContent);
                        break;
                    case ComponentType.RoleSelect:
                        BuildRoleSelect(builder, prop, preFillContent);
                        break;

                    case null:
                        throw new InvalidOperationException($"{prop.Name} of {prop.DeclaringType.Name} isn't a valid modal input field.");
                    default:
                        throw new InvalidOperationException($"Component type {componentType} cannot be used in modals.");
                }
            }

            return builder.Build();
        }

        private static void ReplaceValueInTextComponent(Func<string, ActionRowBuilder> componentByName, string componentName, string newValue)
        {
            var nm = componentByName(componentName);
            var oldComponent = nm.Components[0] as TextInputComponent;
            nm.Components.Clear();

            var textBuilder = new TextInputBuilder()
                .WithCustomId(oldComponent.CustomId)
                .WithLabel(oldComponent.Label)
                .WithPlaceholder(oldComponent.Placeholder)
                .WithStyle(oldComponent.Style)
                .WithValue(newValue);

            if (oldComponent.MinLength.HasValue)
                textBuilder.WithMinLength(oldComponent.MinLength.Value);
            if (oldComponent.MaxLength.HasValue)
                textBuilder.WithMaxLength(oldComponent.MaxLength.Value);
            if (oldComponent.Required.HasValue)
                textBuilder.WithRequired(oldComponent.Required.Value);

            nm.AddComponent(textBuilder);
        }
    }
}
