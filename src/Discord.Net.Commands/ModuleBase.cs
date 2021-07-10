using System;
using System.Threading.Tasks;
using Discord.Commands.Builders;

namespace Discord.Commands
{
    /// <summary>
    ///     Provides a base class for a command module to inherit from.
    /// </summary>
    public abstract class ModuleBase : ModuleBase<ICommandContext> { }

    /// <summary>
    ///     Provides a base class for a command module to inherit from.
    /// </summary>
    /// <typeparam name="T">A class that implements <see cref="ICommandContext"/>.</typeparam>
    public abstract class ModuleBase<T> : IModuleBase
        where T : class, ICommandContext
    {
        /// <summary>
        ///     The underlying context of the command.
        /// </summary>
        /// <seealso cref="T:Discord.Commands.ICommandContext" />
        /// <seealso cref="T:Discord.Commands.CommandContext" />
        public T Context { get; private set; }

        /// <summary>
        ///     Sends a message to the source channel.
        /// </summary>
        /// <param name="message">
        /// Contents of the message; optional only if <paramref name="embed" /> is specified.
        /// </param>
        /// <param name="isTTS">Specifies if Discord should read this <paramref name="message"/> aloud using text-to-speech.</param>
        /// <param name="embed">An embed to be displayed alongside the <paramref name="message"/>.</param>
        /// <param name="allowedMentions">
        ///     Specifies if notifications are sent for mentioned users and roles in the <paramref name="message"/>.
        ///     If <c>null</c>, all mentioned roles and users will be notified.
        /// </param>
        /// <param name="messageReference">The message references to be included. Used to reply to specific messages.</param>
        protected virtual async Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
        {
            return await Context.Channel.SendMessageAsync(message, isTTS, embed, options, allowedMentions, messageReference).ConfigureAwait(false);
        }
        /// <summary>
        ///     The method to execute before executing the command.
        /// </summary>
        /// <param name="command">The <see cref="CommandInfo"/> of the command to be executed.</param>
        protected virtual void BeforeExecute(CommandInfo command)
        {
        }
        /// <summary>
        ///     The method to execute after executing the command.
        /// </summary>
        /// <param name="command">The <see cref="CommandInfo"/> of the command to be executed.</param>
        protected virtual void AfterExecute(CommandInfo command)
        {
        }

        /// <summary>
        ///     The method to execute when an exception is thrown
        /// </summary>
        /// <param name="command"></param>
        /// <param name="exception"></param>
        protected virtual void OnException(CommandInfo command, Exception exception)
        {
        }

        /// <summary>
        ///     The method to execute when building the module.
        /// </summary>
        /// <param name="commandService">The <see cref="CommandService"/> used to create the module.</param>
        /// <param name="builder">The builder used to build the module.</param>
        protected virtual void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
        }

        //IModuleBase
        void IModuleBase.SetContext(ICommandContext context)
        {
            var newValue = context as T;
            Context = newValue ?? throw new InvalidOperationException($"Invalid context type. Expected {typeof(T).Name}, got {context.GetType().Name}.");
        }
        void IModuleBase.BeforeExecute(CommandInfo command) => BeforeExecute(command);
        void IModuleBase.AfterExecute(CommandInfo command) => AfterExecute(command);
        void IModuleBase.OnException(CommandInfo command, Exception exception) => OnException(command, exception);
        void IModuleBase.OnModuleBuilding(CommandService commandService, ModuleBuilder builder) => OnModuleBuilding(commandService, builder);
    }
}
