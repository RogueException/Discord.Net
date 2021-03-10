using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Discord.API.ApplicationCommandInteractionDataOption;

namespace Discord.WebSocket
{
    /// <summary>
    ///     Represents a Websocket-based <see cref="IApplicationCommandInteractionDataOption"/> recieved by the gateway
    /// </summary>
    public class SocketInteractionDataOption : IApplicationCommandInteractionDataOption
    {
        /// <inheritdoc/>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public object Value { get; private set; }

        /// <summary>
        ///      The sub command options recieved for this sub command group.
        /// </summary>
        public IReadOnlyCollection<SocketInteractionDataOption> Options { get; private set; }

        private DiscordSocketClient discord;
        private ulong guild;

        internal SocketInteractionDataOption() { }
        internal SocketInteractionDataOption(Model model, DiscordSocketClient discord, ulong guild)
        {
            this.Name = model.Name;
            this.Value = model.Value.IsSpecified ? model.Value.Value : null;
            this.discord = discord;
            this.guild = guild;

            this.Options = model.Options.IsSpecified
                ? model.Options.Value.Select(x => new SocketInteractionDataOption(x, discord, guild)).ToImmutableArray().ToReadOnlyCollection()
                : null;

        }

        // Converters
        public static explicit operator bool(SocketInteractionDataOption option)
            => (bool)option.Value;
        // The default value is of type long, so an implementaiton of of the long option is trivial
        public static explicit operator int(SocketInteractionDataOption option)
            => unchecked(
            (int)( (long)option.Value )
            );
        public static explicit operator string(SocketInteractionDataOption option)
            => option.Value.ToString();

        public static explicit operator bool?(SocketInteractionDataOption option)
        {
            if (option.Value == null)
                return null;
            else
                return (bool)option;
        }
        public static explicit operator int?(SocketInteractionDataOption option)
        {
            if (option.Value == null)
                return null;
            else
                return (int)option;
        }

        public static explicit operator SocketGuildChannel(SocketInteractionDataOption option)
        {
            if (ulong.TryParse((string)option.Value, out ulong id))
            {
                var guild = option.discord.GetGuild(option.guild);

                if (guild == null)
                    return null;

                return guild.GetChannel(id);
            }

            return null;
        }

        public static explicit operator SocketRole(SocketInteractionDataOption option)
        {
            if (ulong.TryParse((string)option.Value, out ulong id))
            {
                var guild = option.discord.GetGuild(option.guild);

                if (guild == null)
                    return null;

                return guild.GetRole(id);
            }

            return null;
        }

        public static explicit operator SocketGuildUser(SocketInteractionDataOption option)
        {
            if (ulong.TryParse((string)option.Value, out ulong id))
            {
                var guild = option.discord.GetGuild(option.guild);

                if (guild == null)
                    return null;

                return guild.GetUser(id);
            }

            return null;
        }

        IReadOnlyCollection<IApplicationCommandInteractionDataOption> IApplicationCommandInteractionDataOption.Options => this.Options;
    }
}
