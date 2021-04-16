using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Net
{
    public class ApplicationCommandException : Exception
    {
        /// <summary>
        ///     Gets the JSON error code returned by Discord.
        /// </summary>
        /// <returns>
        ///     A 
        ///     <see href="https://discord.com/developers/docs/topics/opcodes-and-status-codes#json">JSON error code</see>
        ///     from Discord, or <c>null</c> if none.
        /// </returns>
        public int? DiscordCode { get; }

        /// <summary>
        ///     Gets the reason of the exception.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        ///     Gets the request object used to send the request.
        /// </summary>
        public IRequest Request { get; }

        /// <summary>
        ///     The error object returned from discord.
        /// </summary>
        /// <remarks>
        ///     Note: This object can be null if discord didn't provide it.
        /// </remarks>
        public object Error { get; }

        /// <summary>
        ///     The request json used to create the application command. This is useful for checking your commands for any format errors.
        /// </summary>
        public string RequestJson { get; }

        /// <summary>
        ///     The underlying <see cref="HttpException"/> that caused this exception to be thrown.
        /// </summary>
        public HttpException InnerHttpException { get; }
        /// <summary>
        ///     Initializes a new instance of the <see cref="ApplicationCommandException" /> class.
        /// </summary>
        /// <param name="request">The request that was sent prior to the exception.</param>
        /// <param name="requestJson"></param>
        /// <param name="httpError"></param>
        /// <param name="discordCode">The Discord status code returned.</param>
        /// <param name="reason">The reason behind the exception.</param>
        /// <param name="errors"></param>
        public ApplicationCommandException(string requestJson, HttpException httpError)
            : base("The application command failed to be created!", httpError)
        {
            Request = httpError.Request;
            DiscordCode = httpError.DiscordCode;
            Reason = httpError.Reason;
            Error = httpError.Error;
            RequestJson = requestJson;
            InnerHttpException = httpError;
        }
    }
}
