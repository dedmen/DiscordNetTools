using DiscordNetTools.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordNetTools.Modal
{
    public class ModalDirectory
    {
        public static ModalDirectory Instance;
        private Dictionary<string, Type> ModalTypes = new Dictionary<string, Type>();
        private Dictionary<Type, Type> ModalToMultiModal = new Dictionary<Type, Type>();
        private Dictionary<string, Type> ComponentInteractionToMultiModal = new Dictionary<string, Type>();

        public ModalDirectory()
        {
            Instance = this;
            var modalTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IModalWithValidation).IsAssignableFrom(p));

            foreach (var type1 in modalTypes.Where(x => !x.IsAbstract))
            {
                ModalTypes.Add(type1.Name, type1);

                if (type1.GetCustomAttributes(typeof(MultiStepInteractionAssociationAttribute), true).First() is MultiStepInteractionAssociationAttribute attrib)
                {
                    ModalToMultiModal.Add(type1, attrib.MultiModalType);
                }
            }

            var multiStepModalTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IMultiStepInteraction).IsAssignableFrom(p));


            foreach (var type1 in multiStepModalTypes.Where(x => !x.IsAbstract))
            {
                ModalTypes.Add(type1.Name, type1);

                foreach (var attrib in type1.GetCustomAttributes(typeof(MultiStepComponentInteractionAssociationAttribute), true).Select(x => x as MultiStepComponentInteractionAssociationAttribute))
                {
                    ComponentInteractionToMultiModal.Add(attrib.ComponentInteractionId, type1);
                }
            }
        }


        public Type GetModalType(string modalName)
        {
            return ModalTypes[modalName];
        }

        public Type GetMultiModalTypeFromModal(Type modalType)
        {
            return ModalToMultiModal.TryGetValue(modalType, out var result) ? result : null;
        }
        public Type GetMultiModalTypeFromComponentInteractionId(string componentInteractionId)
        {
            componentInteractionId = InteractionContext.GetBaseInteractId(componentInteractionId);
            return ComponentInteractionToMultiModal.TryGetValue(componentInteractionId, out var result) ? result : null;
        }

    }
}
