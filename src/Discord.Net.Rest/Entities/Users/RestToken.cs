using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Discord.API.Rest.Token;

namespace Discord.Rest
{
    public class RestToken
    {
        public string Token { get; set; }
        public TokenType TokenType { get; set; }
        public DateTimeOffset ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
        public IEnumerable<string> Scopes { get; set; }

        internal static RestToken Create(BaseDiscordClient discord, Model model)
        {
            return new RestToken { ExpiresIn = DateTimeOffset.UtcNow.AddSeconds(model.ExpiresIn), Token = model.AccessToken, TokenType = TokenType.Bearer, RefreshToken = model.RefreshToken, Scopes = model.Scope.Split(' ') };
        }
    }
}
