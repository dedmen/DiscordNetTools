using Discord;
using System.Diagnostics;

namespace DiscordNetTools.Context
{
    /// <summary>
    /// Every Discord interaction gets a unique ID, with which a global context is kept to store data between multiple related interactions
    /// </summary>
    public class InteractionContext
    {
        private static readonly Dictionary<string, InteractionContext> ActiveContexts = new();
        private static readonly Random Random = new Random();
        private static readonly System.Timers.Timer CleanupTaskTimer = new(60000) { AutoReset = false };

        public static (string interactionIdBase, string contextToken) SplitInteractionId(string interactionId)
        {
            var split = interactionId.Split(':');
            if (split.Length == 2) return (split[0], split[1]);
            return (split[0], "");
        }

        public static string GetContextTokenFromInteractionId(string interactionId)
        {
            return SplitInteractionId(interactionId).contextToken;
        }
        public static string GetBaseInteractId(string interactionId)
        {
            return SplitInteractionId(interactionId).interactionIdBase;
        }

        //public static string AppendContextToken(IInteractionContext context, string name)
        //{
        //    if (Debugger.IsAttached && name.Contains(':')) Debugger.Break();
        //
        //    if (context.Interaction == null) return name;
        //    if (context.Interaction is IComponentInteraction compInteraction)
        //    {
        //        return $"{name}:{compInteraction.Data.CustomId}";
        //    }
        //    return name;
        //}

        // Storage of all contexts

        private static string CreateNewToken()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            //!§$%&/()=?{[]}@#+-
            string newToken;

            lock (ActiveContexts)
            {
                do
                {
                    newToken = new string(Enumerable.Repeat(chars, 8)
                        .Select(s => s[Random.Next(s.Length)]).ToArray());
                } while (ActiveContexts.ContainsKey(newToken));
            }
            return newToken;
        }

        public static InteractionContext Get(Discord.IInteractionContext existingContext)
        {
            string token = null;
            switch (existingContext.Interaction)
            {
                case Discord.IComponentInteraction compInteraction:
                    token = GetContextTokenFromInteractionId(compInteraction.Data.CustomId);
                    break;
                case Discord.IModalInteraction modInteraction:
                    token = GetContextTokenFromInteractionId(modInteraction.Data.CustomId);
                    break;
            }

            {
                if (existingContext is SlashMessageInteractionContext slashMessageInteraction && slashMessageInteraction.InteractionContextToken != null)
                    token = slashMessageInteraction.InteractionContextToken;
            }

            if (string.IsNullOrEmpty(token))
            {
                // create new token
                token = CreateNewToken();

                if (existingContext is SlashMessageInteractionContext slashMessageInteraction)
                    slashMessageInteraction.InteractionContextToken = token;
            }

            lock (ActiveContexts)
            {
                if (ActiveContexts.TryGetValue(token, out var context))
                {
                    Console.WriteLine($"Found context for {context._userId} in {context._channelId}");
                    context._lastActivity = DateTime.Now;
                    return context;
                }

                InteractionContext newResult = new InteractionContext(token: token, userId: existingContext.User.Id, channelId: existingContext.Channel.Id);
                Console.WriteLine($"Create context for {newResult._userId} in {newResult._channelId}");
                ActiveContexts.Add(token, newResult);

                if (!CleanupTaskTimer.AutoReset) // Missusing this as a "is initialized" flag
                {
                    CleanupTaskTimer.AutoReset = true;
                    CleanupTaskTimer.Elapsed += (x, y) => { CleanupCycle(); };
                }
                CleanupTaskTimer.Start();

                return newResult;
            }
        }

        private static void CleanupCycle()
        {
            lock (ActiveContexts)
            {
                var toDelete = ActiveContexts.Where(x => (DateTime.Now - x.Value._lastActivity) > TimeSpan.FromMinutes(15));
                foreach (var (key, value) in toDelete)
                {
                    CloseContext(value, ContextCloseReason.Timeout);
                    Console.WriteLine($"Cleanup context for {value._userId} in {value._channelId}");
                }
                if (ActiveContexts.Count == 0) CleanupTaskTimer.Stop();
            }
        }

        public enum ContextCloseReason
        {
            Finished,
            Timeout
        }

        public static void CloseContext(InteractionContext context, ContextCloseReason reason = ContextCloseReason.Finished)
        {
            Console.WriteLine($"CloseContext for {context._userId} because {reason}");
            lock (ActiveContexts)
            {
                ActiveContexts.Remove(context._token);
            }
            foreach (var (key, value) in context._data)
            {
                if (value is IInteractionContextWithExitHandler exitHandler)
                    exitHandler.OnBeforeClose(reason);
            }
            context._data.Clear();
        }

        public interface IInteractionContextWithExitHandler
        {
            void OnBeforeClose(ContextCloseReason reason);
        }

        private readonly Dictionary<Type, object> _data = new();
        private readonly string _token;
        private DateTime _lastActivity = DateTime.Now;
        private readonly UInt64 _userId;
        private readonly UInt64 _channelId;

        public InteractionContext(string token, ulong userId, ulong channelId)
        {
            _token = token;
            _userId = userId;
            _channelId = channelId;
        }

        public T? Get<T>() where T : class
        {
            if (_data.TryGetValue(typeof(T), out var result))
                return (T)result;
            return null;
        }

        public T GetOrCreate<T>() where T : class, new()
        {
            if (_data.TryGetValue(typeof(T), out var result))
                return (T)result;
            var newResult = new T();
            _data.Add(typeof(T), newResult);
            return newResult;
        }

        public T Set<T>(T val) where T : class
        {
            if (_data.TryAdd(typeof(T), val)) return val;

            _data.Remove(typeof(T));
            _data.Add(typeof(T), val);
            return val;
        }

        public string WrapInteraction(string interactionId)
        {
            if (Debugger.IsAttached && interactionId.Contains(':')) Debugger.Break();
            return $"{interactionId}:{_token}";
        }

        public Discord.Modal WrapInteraction(Discord.Modal modal)
        {
            modal.CustomId = WrapInteraction(modal.CustomId);
            return modal;
        }
    }
}
