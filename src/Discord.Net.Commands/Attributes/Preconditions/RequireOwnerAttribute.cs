using System;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Commands
{
    /// <summary>
    ///     Requires the command to be invoked by the owner of the bot.
    /// </summary>
    /// <remarks>
    ///     This precondition will restrict the access of the command or module to the owner of the Discord application.
    ///     If the precondition fails to be met, an erroneous <see cref="PreconditionResult"/> will be returned with the
    ///     message "Command can only be run by the owner of the bot."
    ///     <note>
    ///     This precondition will only work if the account has a <see cref="TokenType"/> of <see cref="TokenType.Bot"/>
    ///     ;otherwise, this precondition will always fail.
    ///     </note>
    /// </remarks>
    /// <example>
    ///     The following example restricts the command to a set of sensitive commands that only the owner of the bot
    ///     application should be able to access.
    ///     <code language="cs">
    ///     [RequireOwner]
    ///     [Group("admin")]
    ///     public class AdminModule : ModuleBase
    ///     {
    ///         [Command("exit")]
    ///         public async Task ExitAsync()
    ///         {
    ///             Environment.Exit(0);
    ///         }
    ///     }
    ///     </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireOwnerAttribute : PreconditionAttribute
    {
        /// <inheritdoc />
        public override string ErrorMessage { get; set; }

        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            switch (context.Client.TokenType)
            {
                case TokenType.Bot:
                    var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

                    if (application.Team?.TeamMembers.All(t => t.User.Id != context.User.Id) ?? context.User.Id != application.Owner.Id)
                        return PreconditionResult.FromError(ErrorMessage ?? "Command can only be run by the owner of the bot.");

                    return PreconditionResult.FromSuccess();
                default:
                    return PreconditionResult.FromError($"{nameof(RequireOwnerAttribute)} is not supported by this {nameof(TokenType)}.");
            }
        }
    }
}
