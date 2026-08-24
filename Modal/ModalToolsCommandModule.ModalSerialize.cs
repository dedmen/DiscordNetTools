using Discord;
using Discord.Interactions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

        private static void BuildSelectMenu(ModalBuilder modalBuilder, PropertyInfo propertyInfo, UserDiscordModalContent? userDiscordModalContent)
        {
            var attributes = propertyInfo.GetCustomAttributes();

            string label = propertyInfo.Name;
            string customId = null;
            string placeholder = null;
            int minValues = 1;
            int maxValues = 1;
            bool isRequired = true;
            bool disabled = false;
            ComponentType type = ComponentType.SelectMenu; // This
            SelectMenuDefaultValue[] defaultValues = null;
            int? id = null;
            string description = null;
            int? labelId = null;
            List<SelectMenuOptionBuilder> options = new();

            var builder = new SelectMenuBuilder();

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case ModalSelectMenuAttribute textInput:

                        customId = textInput.CustomId;
                        placeholder = textInput.Placeholder;
                        minValues = textInput.MinValues;
                        maxValues = textInput.MaxValues;
                        id = textInput.Id;

                        break;
                    case RequiredInputAttribute requiredInput:
                        isRequired = requiredInput.IsRequired;
                        break;
                    case InputLabelAttribute inputLabel:
                        label = inputLabel.Label;
                        description = inputLabel.Description;
                        break;

                    case ModalSelectMenuOptionAttribute option:
                        options.Add(new SelectMenuOptionBuilder(option.Label, option.Value, option.Description, Discord.Emote.Parse(option.Emote), option.IsDefault));
                        break;
                    default:
                        break;
                }
            }

            // The prefill content, might have some options
            var prefillData = userDiscordModalContent as UserDiscordModalContentWithExtraData;
            if (prefillData != null)
            {
                if (prefillData.ExtraData.TryGetValue(customId, out var optionsFillData))
                {
                    Debug.Assert(optionsFillData as IEnumerable<SelectMenuOptionBuilder> != null);
                    options.AddRange(optionsFillData as IEnumerable<SelectMenuOptionBuilder>);
                }
            }


            modalBuilder.AddSelectMenu(label, customId, options, placeholder, minValues, maxValues, isRequired, disabled, type, null, defaultValues, id, description, labelId);
        }

        private static void BuildCheckbox(ModalBuilder modalBuilder, PropertyInfo propertyInfo, UserDiscordModalContent? userDiscordModalContent)
        {
            var attributes = propertyInfo.GetCustomAttributes();

            string label = propertyInfo.Name;
            string customId = null;
            bool? defaultState = null;
            int? id = null;
            string? description = null;
            int? labelId = null;

            var builder = new SelectMenuBuilder();

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case Discord.Interactions.ModalCheckboxAttribute textInput:
                        customId = textInput.CustomId;
                        id = textInput.Id;

                        var fieldName = propertyInfo.GetCustomAttribute(typeof(JsonPropertyNameAttribute)) is JsonPropertyNameAttribute jsonNameAttrib ? jsonNameAttrib.Name : propertyInfo.Name;
                        var fieldValue = userDiscordModalContent?.GetFieldContent(propertyInfo.Name) ?? "";

                        if (bool.TryParse(fieldValue, out var defaultVal))
                            defaultState = defaultVal;

                        break;
                    case RequiredInputAttribute { IsRequired: false }:
                        // required: true => noop
                        throw new InvalidOperationException("Checkbox component cannot be set as optional, see Discord API documentation for more information.");
                    case InputLabelAttribute inputLabel:
                        label = inputLabel.Label;
                        description = inputLabel.Description;
                        break;
                    case DefaultValueAttribute defaultValue:
                        defaultState ??= (bool)defaultValue.Value; //Only null assign, because if its non-null, then it was already filled by prefill content
                        break;
                    default:
                        break;
                }
            }

            //#TODO prefill default state

            modalBuilder.AddCheckBox(label, customId, defaultState, description, labelId);
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
                    case ComponentType.SelectMenu: // All select options, must be provided by preFillContent
                        BuildSelectMenu(builder, prop, preFillContent);
                        break;

                    case ComponentType.Checkbox: // All select options, must be provided by preFillContent
                        BuildCheckbox(builder, prop, preFillContent);
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
