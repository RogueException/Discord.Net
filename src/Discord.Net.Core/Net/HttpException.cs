using System;
using System.Net;

namespace Discord.Net
{
    /// <summary>
    ///     The exception that is thrown if an error occurs while processing an Discord HTTP request.
    /// </summary>
    public class HttpException : Exception
    {
        /// <summary>
        ///     Gets the HTTP status code returned by Discord.
        /// </summary>
        /// <returns>
        ///     An 
        ///     <see href="https://discord.com/developers/docs/topics/opcodes-and-status-codes#http">HTTP status code</see>
        ///     from Discord.
        /// </returns>
        public HttpStatusCode HttpCode { get; }
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
        ///     Initializes a new instance of the <see cref="HttpException" /> class.
        /// </summary>
        /// <param name="httpCode">The HTTP status code returned.</param>
        /// <param name="request">The request that was sent prior to the exception.</param>
        /// <param name="discordCode">The Discord status code returned.</param>
        /// <param name="reason">The reason behind the exception.</param>
        public HttpException(HttpStatusCode httpCode, IRequest request, int? discordCode = null, string reason = null, object errors = null)
            : base(CreateMessage(httpCode, discordCode, reason))
        {
            HttpCode = httpCode;
            Request = request;
            DiscordCode = discordCode;
            Reason = reason;
            Error = errors;
        }

        private static string CreateMessage(HttpStatusCode httpCode, int? discordCode = null, string reason = null)
        {   
            string msg;
            if (discordCode != null && discordCode != 0)
            {
                if (reason != null)
                    msg = $"The server responded with error {(int)discordCode}: {reason}";
                else
                    msg = $"The server responded with error {(int)discordCode}: {httpCode}";
            }
            else
            {
                if (reason != null)
                    msg = $"The server responded with error {(int)httpCode}: {reason}";
                else
                    msg = $"The server responded with error {(int)httpCode}: {httpCode}";
            }
            return msg;
        }
    }
}
