using Discord.Rest;
using Discord;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _05_simple_blazor_discord_login.Pages
{
    public partial class Index : ComponentBase
    {
        [Inject]
        private DiscordRestClient DiscordRestClient { get; set; }
        [Inject]
        private NavigationManager navigationManager { get; set; }
        public RestSelfUser User { get; set; }
        public RestToken RestToken { get; set; }

        protected override async Task OnInitializedAsync()
        {
            
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
                var uri_query = uri.Query;
                //?access_token = RTfP0OK99U3kbRtHOjjLmjbOn45PjL & token_type = Bearer & expires_in = 604800 & scope = identify & state = 15773059ghq9183habn
                if (QueryHelpers.ParseQuery(uri.Fragment.TrimStart('#')).TryGetValue("access_token", out var token))
                {

                    await DiscordRestClient.LoginAsync(TokenType.Bearer, token); //this can give you an exception if the token is expired!
                    User = DiscordRestClient.CurrentUser;
                    StateHasChanged();
                }
                if (QueryHelpers.ParseQuery(uri.Query.TrimStart('?')).TryGetValue("code", out var code))
                {
                    RestToken = await DiscordRestClient.GetTokenAsync(TokenType.Code, code, navigationManager.BaseUri, new List<string> { "identify" }); //this can give you an exception if the token is expired!
                    await DiscordRestClient.LoginAsync(TokenType.Bearer, RestToken.Token);
                    User = DiscordRestClient.CurrentUser;
                    StateHasChanged();
                }
                if (QueryHelpers.ParseQuery(uri.Query.TrimStart('?')).TryGetValue("refresh_token", out var refreshToken))
                {
                    RestToken = await DiscordRestClient.GetTokenAsync(TokenType.Refresh, refreshToken, navigationManager.BaseUri, new List<string> { "identify" }); //this can give you an exception if the token is expired!
                    await DiscordRestClient.LoginAsync(TokenType.Bearer, RestToken.Token);
                    User = DiscordRestClient.CurrentUser;
                    StateHasChanged();
                }
            }
            
        }


    }

}
