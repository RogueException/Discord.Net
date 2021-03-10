using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Discord.API.ApplicationCommandOption;

namespace Discord.WebSocket
{
    /// <summary>
    ///     Represents an option for a <see cref="SocketApplicationCommand"/>.
    /// </summary>
    public class SocketApplicationCommandOption : IApplicationCommandOption
    {
        /// <inheritdoc/>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public ApplicationCommandOptionType Type { get; private set; }

        /// <inheritdoc/>
        public string Description { get; private set; }

        /// <inheritdoc/>
        public bool? Default { get; private set; }

        /// <inheritdoc/>
        public bool? Required { get; private set; }

        /// <summary>
        ///     Choices for string and int types for the user to pick from.
        /// </summary>
        public IReadOnlyCollection<SocketApplicationCommandChoice> Choices { get; private set; }

        /// <summary>
        ///     If the option is a subcommand or subcommand group type, this nested options will be the parameters.
        /// </summary>
        public IReadOnlyCollection<SocketApplicationCommandOption> Options { get; private set; }

        internal SocketApplicationCommandOption() { }
        internal static SocketApplicationCommandOption Create(Model model)
        {
            var entity = new SocketApplicationCommandOption();
            entity.Update(model);
            return entity;
        }

        internal void Update(Model model)
        {
            this.Name = model.Name;
            this.Type = model.Type;
            this.Description = model.Description;

            this.Default = model.Default.IsSpecified
                ? model.Default.Value
                : (bool?)null;

            this.Required = model.Required.IsSpecified
                ? model.Required.Value
                : (bool?)null;

            this.Choices = model.Choices.IsSpecified
                ? model.Choices.Value.Select(x => SocketApplicationCommandChoice.Create(x)).ToImmutableArray().ToReadOnlyCollection()
                : null;

            this.Options = model.Options.IsSpecified
                ? model.Options.Value.Select(x => SocketApplicationCommandOption.Create(x)).ToImmutableArray().ToReadOnlyCollection()
                : null;
        }

        IReadOnlyCollection<IApplicationCommandOptionChoice> IApplicationCommandOption.Choices => Choices;
        IReadOnlyCollection<IApplicationCommandOption> IApplicationCommandOption.Options => Options;
    }
}
