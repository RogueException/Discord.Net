using System;

namespace Discord
{
    /// <summary> Specifies the type of token to use with the client. </summary>
    public enum TokenType
    {
        [Obsolete("User logins are deprecated and may result in a ToS strike against your account - please see https://github.com/RogueException/Discord.Net/issues/827", error: true)]
        User,
        /// <summary>
        ///     An OAuth2 token type.
        /// </summary>
        Bearer,
        /// <summary>
        ///     A bot token type.
        /// </summary>
        Bot,
        /// <summary>
        ///     A webhook token type.
        /// </summary>
        Webhook,
        ///<summary>
        ///     A refresh token used to get a new access token.
        /// </summary>
        Refresh,
        ///<summary>
        ///     The code used to get a Token through Oauth2
        /// </summary>
        Code

    }
}
