using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Discord.API.ApplicationCommandOptionChoice;

namespace Discord.WebSocket
{
    /// <summary>
    ///     Represents a choice for a <see cref="SocketApplicationCommandOption"/>.
    /// </summary>
    public class SocketApplicationCommandChoice : IApplicationCommandOptionChoice
    {
        /// <inheritdoc/>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public object Value { get; private set; }

        internal SocketApplicationCommandChoice() { }
        internal static SocketApplicationCommandChoice Create(Model model)
        {
            var entity = new SocketApplicationCommandChoice();
            entity.Update(model);
            return entity;
        }
        internal void Update(Model model)
        {
            this.Name = model.Name;
            this.Value = model.Value;
        }
    }
}
