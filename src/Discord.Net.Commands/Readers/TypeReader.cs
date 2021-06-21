using System;
using System.Threading.Tasks;

namespace Discord.Commands
{
    /// <summary>
    ///     Defines a reader class that parses user input into a specified type.
    /// </summary>
    public abstract class TypeReader
    {
        internal bool IsOverride { get; set; } = false;
        /// <summary>
        ///     Attempts to parse the <paramref name="input"/> into the desired type.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="input">The raw input of the command.</param>
        /// <param name="services">The service collection used for dependency injection.</param>
        /// <returns>
        ///     A task that represents the asynchronous parsing operation. The task result contains the parsing result.
        /// </returns>
        public abstract Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services);
    }
}
