using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     Represents a <see cref="IApplicationCommandOption"/> for making slash commands.
    /// </summary>
    public class ApplicationCommandOptionProperties
    {
        private string _name;
        private string _description;

        /// <summary>
        ///     The name of this option.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value?.Length > 32)
                    throw new ArgumentException("Name length must be less than or equal to 32");
                _name = value;
            }
        }

        /// <summary>
        ///     The description of this option.
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (value?.Length > 100)
                    throw new ArgumentException("Name length must be less than or equal to 32");
                _description = value;
            }
        }

        /// <summary>
        ///     The type of this option.
        /// </summary>
        public ApplicationCommandOptionType Type { get; set; }

        /// <summary>
        ///     The first required option for the user to complete. only one option can be default.
        /// </summary>
        public bool? Default { get; set; }

        /// <summary>
        ///     <see langword="true"/> if this option is required for this command, otherwise <see langword="false"/>.
        /// </summary>
        public bool? Required { get; set; }

        /// <summary>
        ///     choices for string and int types for the user to pick from.
        /// </summary>
        public List<ApplicationCommandOptionChoiceProperties> Choices { get; set; }

        /// <summary>
        ///     If the option is a subcommand or subcommand group type, this nested options will be the parameters.
        /// </summary>
        public List<ApplicationCommandOptionProperties> Options { get; set; }

        
    }
}
