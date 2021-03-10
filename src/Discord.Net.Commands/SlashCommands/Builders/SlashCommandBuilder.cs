using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Discord.Commands.Builders
{
    /// <summary>
    ///     A class used to build slash commands.
    /// </summary>
    public class SlashCommandBuilder
    {
        /// <summary> 
        ///     Returns the maximun length a commands name allowed by Discord
        /// </summary>
        public const int MaxNameLength = 32;
        /// <summary> 
        ///     Returns the maximum length of a commands description allowed by Discord. 
        /// </summary>
        public const int MaxDescriptionLength = 100;
        /// <summary> 
        ///     Returns the maximum count of command options allowed by Discord
        /// </summary>
        public const int MaxOptionsCount = 10;

        /// <summary>
        ///     The name of this slash command.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                Preconditions.NotNullOrEmpty(value, nameof(Name));
                Preconditions.AtLeast(value.Length, 3, nameof(Name));
                Preconditions.AtMost(value.Length, MaxNameLength, nameof(Name));

                // Discord updated the docs, this regex prevents special characters like @!$%(... etc,
                // https://discord.com/developers/docs/interactions/slash-commands#applicationcommand
                if (!Regex.IsMatch(value, @"^[\w-]{3,32}$"))
                    throw new ArgumentException("Command name cannot contian any special characters or whitespaces!");

                _name = value;
            }
        }

        /// <summary>
        ///    A 1-100 length description of this slash command
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                Preconditions.AtLeast(value.Length, 1, nameof(Description));
                Preconditions.AtMost(value.Length, MaxDescriptionLength, nameof(Description));

                _description = value;
            }
        }

        public ulong GuildId
        {
            get
            {
                return _guildId ?? 0;
            }
            set
            {
                if (value == 0)
                {
                    throw new ArgumentException("Guild ID cannot be 0!");
                }

                _guildId = value;

                if (isGlobal)
                    isGlobal = false;
            }
        }
        /// <summary>
        ///     Gets or sets the options for this command.
        /// </summary>
        public List<SlashCommandOptionBuilder> Options
        {
            get
            {
                return _options;
            }
            set
            {
                if (value != null)
                    if (value.Count > MaxOptionsCount)
                        throw new ArgumentException(message: $"Option count must be less than or equal to {MaxOptionsCount}.", paramName: nameof(Options));

                _options = value;
            }
        }

        private ulong? _guildId { get; set; }
        private string _name { get; set; }
        private string _description { get; set; }
        private List<SlashCommandOptionBuilder> _options { get; set; }

        internal bool isGlobal { get; set; }


        public SlashCommandCreationProperties Build()
        {
            SlashCommandCreationProperties props = new SlashCommandCreationProperties()
            {
                Name = this.Name,
                Description = this.Description,
            };

            if(this.Options != null || this.Options.Any())
            {
                var options = new List<ApplicationCommandOptionProperties>();

                this.Options.ForEach(x => options.Add(x.Build()));

                props.Options = options;
            }

            return props;

        }

        /// <summary>
        ///     Makes this command a global application command .
        /// </summary>
        /// <returns>The current builder.</returns>
        public SlashCommandBuilder MakeGlobal()
        {
            this.isGlobal = true;
            return this;
        }

        /// <summary>
        ///     Makes this command a guild specific command.
        /// </summary>
        /// <param name="GuildId">The Id of the target guild.</param>
        /// <returns>The current builder.</returns>
        public SlashCommandBuilder ForGuild(ulong GuildId)
        {
            this.GuildId = GuildId;
            return this;
        }

        public SlashCommandBuilder WithName(string Name)
        {
            this.Name = Name;
            return this;
        }

        /// <summary>
        ///     Sets the description of the current command.
        /// </summary>
        /// <param name="Description">The description of this command.</param>
        /// <returns>The current builder.</returns>
        public SlashCommandBuilder WithDescription(string Description)
        {
            this.Description = Description;
            return this;
        }

        /// <summary>
        ///     Adds an option to the current slash command.
        /// </summary>
        /// <param name="Name">The name of the option to add.</param>
        /// <param name="Type">The type of this option.</param>
        /// <param name="Description">The description of this option.</param>
        /// <param name="Required">If this option is required for this command.</param>
        /// <param name="Default">If this option is the default option.</param>
        /// <param name="Options">The options of the option to add.</param>
        /// <param name="Choices">The choices of this option.</param>
        /// <returns>The current builder.</returns>
        public SlashCommandBuilder AddOption(string Name, ApplicationCommandOptionType Type,
           string Description, bool Required = true, bool Default = false, List<SlashCommandOptionBuilder> Options = null, params ApplicationCommandOptionChoiceProperties[] Choices)
        {
            // Make sure the name matches the requirements from discord
            Preconditions.NotNullOrEmpty(Name, nameof(Name));
            Preconditions.AtLeast(Name.Length, 3, nameof(Name));
            Preconditions.AtMost(Name.Length, MaxNameLength, nameof(Name));

            // Discord updated the docs, this regex prevents special characters like @!$%( and s p a c e s.. etc,
            // https://discord.com/developers/docs/interactions/slash-commands#applicationcommand
            if (!Regex.IsMatch(Name, @"^[\w-]{3,32}$"))
                throw new ArgumentException("Command name cannot contian any special characters or whitespaces!", nameof(Name));

            // same with description
            Preconditions.NotNullOrEmpty(Description, nameof(Description));
            Preconditions.AtLeast(Description.Length, 3, nameof(Description));
            Preconditions.AtMost(Description.Length, MaxDescriptionLength, nameof(Description));

            // make sure theres only one option with default set to true
            if (Default)
            {
                if (this.Options != null)
                    if (this.Options.Any(x => x.Default))
                        throw new ArgumentException("There can only be one command option with default set to true!", nameof(Default));
            }

            SlashCommandOptionBuilder option = new SlashCommandOptionBuilder();
            option.Name = Name;
            option.Description = Description;
            option.Required = Required;
            option.Default = Default;
            option.Options = Options;
            option.Choices = Choices != null ? new List<ApplicationCommandOptionChoiceProperties>(Choices) : null;

            return AddOption(option);
        }

        /// <summary>
        ///     Adds an option to the current slash command.
        /// </summary>
        /// <param name="Name">The name of the option to add.</param>
        /// <param name="Type">The type of this option.</param>
        /// <param name="Description">The description of this option.</param>
        /// <param name="Required">If this option is required for this command.</param>
        /// <param name="Default">If this option is the default option.</param>
        /// <param name="Choices">The choices of this option.</param>
        /// <returns>The current builder.</returns>
        public SlashCommandBuilder AddOption(string Name, ApplicationCommandOptionType Type,
            string Description, bool Required = true, bool Default = false, params ApplicationCommandOptionChoiceProperties[] Choices)
            => AddOption(Name, Type, Description, Required, Default, null, Choices);

        /// <summary>
        ///     Adds an option to the current slash command.
        /// </summary>
        /// <param name="Name">The name of the option to add.</param>
        /// <param name="Type">The type of this option.</param>
        /// <param name="Description">The sescription of this option.</param>
        /// <returns>The current builder.</returns>
        public SlashCommandBuilder AddOption(string Name, ApplicationCommandOptionType Type, string Description)
            => AddOption(Name, Type, Description, Options: null, Choices: null);

        /// <summary>
        ///     Adds an option to this slash command.
        /// </summary>
        /// <param name="Parameter">The option to add.</param>
        /// <returns>The current builder.</returns>
        public SlashCommandBuilder AddOption(SlashCommandOptionBuilder Option)
        {
            if (this.Options == null)
                this.Options = new List<SlashCommandOptionBuilder>();

            if (this.Options.Count >= MaxOptionsCount)
                throw new ArgumentOutOfRangeException(nameof(Options), $"Cannot have more than {MaxOptionsCount} options!");

            if (Option == null)
                throw new ArgumentNullException(nameof(Option), "Option cannot be null");

            this.Options.Add(Option);
            return this;
        }
        /// <summary>
        ///     Adds a collection of options to the current slash command.
        /// </summary>
        /// <param name="Parameter">The collection of options to add.</param>
        /// <returns>The current builder.</returns>
        public SlashCommandBuilder AddOptions(params SlashCommandOptionBuilder[] Options)
        {
            if (Options == null)
                throw new ArgumentNullException(nameof(Options), "Options cannot be null!");

            if (Options.Length == 0)
                throw new ArgumentException(nameof(Options), "Options cannot be empty!");

            if (this.Options == null)
                this.Options = new List<SlashCommandOptionBuilder>();

            if (this.Options.Count + Options.Length > MaxOptionsCount)
                throw new ArgumentOutOfRangeException(nameof(Options), $"Cannot have more than {MaxOptionsCount} options!");

            this.Options.AddRange(Options);
            return this;
        }
    }

    /// <summary>
    ///     Represents a class used to build options for the <see cref="SlashCommandBuilder"/>.
    /// </summary>
    public class SlashCommandOptionBuilder
    {
        /// <summary>
        ///     The max length of a choice's name allowed by Discord.
        /// </summary>
        public const int ChoiceNameMaxLength = 100;

        /// <summary>
        ///     The maximum number of choices allowed by Discord.
        /// </summary>
        public const int MaxChoiceCount = 10;

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
                if (value?.Length > SlashCommandBuilder.MaxNameLength)
                    throw new ArgumentException("Name length must be less than or equal to 32");
                if(value?.Length < 3)
                    throw new ArgumentException("Name length must at least 3 characters in length");

                // Discord updated the docs, this regex prevents special characters like @!$%(... etc,
                // https://discord.com/developers/docs/interactions/slash-commands#applicationcommand
                if (!Regex.IsMatch(value, @"^[\w-]{3,32}$"))
                    throw new ArgumentException("Option name cannot contian any special characters or whitespaces!");

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
                if (value?.Length > SlashCommandBuilder.MaxDescriptionLength)
                    throw new ArgumentException("Description length must be less than or equal to 100");
                if (value?.Length < 1)
                    throw new ArgumentException("Name length must at least 1 character in length");

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
        public bool Default { get; set; }

        /// <summary>
        ///     <see langword="true"/> if this option is required for this command, otherwise <see langword="false"/>.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        ///     choices for string and int types for the user to pick from.
        /// </summary>
        public List<ApplicationCommandOptionChoiceProperties> Choices { get; set; }

        /// <summary>
        ///     If the option is a subcommand or subcommand group type, this nested options will be the parameters.
        /// </summary>
        public List<SlashCommandOptionBuilder> Options { get; set; }

        /// <summary>
        ///     Builds the current option.
        /// </summary>
        /// <returns>The build version of this option</returns>
        public ApplicationCommandOptionProperties Build()
        {
            bool isSubType = this.Type == ApplicationCommandOptionType.SubCommand || this.Type == ApplicationCommandOptionType.SubCommandGroup;

            if (this.Type == ApplicationCommandOptionType.SubCommandGroup && (Options == null || !Options.Any()))
                throw new ArgumentException(nameof(Options), "SubCommandGroups must have at least one option");

            if (!isSubType && (Options != null && Options.Any()))
                throw new ArgumentException(nameof(Options), $"Cannot have options on {Type} type");

            return new ApplicationCommandOptionProperties()
            {
                Name = this.Name,
                Description = this.Description,
                Default = this.Default,
                Required = this.Required,
                Type = this.Type,
                Options = Options != null ? new List<ApplicationCommandOptionProperties>(this.Options.Select(x => x.Build())) : null,
                Choices = this.Choices
            };
        }

        /// <summary>
        ///     Adds a sub 
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public SlashCommandOptionBuilder AddOption(SlashCommandOptionBuilder option)
        {
            if (this.Options == null)
                this.Options = new List<SlashCommandOptionBuilder>();

            if (this.Options.Count >= SlashCommandBuilder.MaxOptionsCount)
                throw new ArgumentOutOfRangeException(nameof(Choices), $"There can only be {SlashCommandBuilder.MaxOptionsCount} options per sub command group!");

            if (option == null)
                throw new ArgumentNullException(nameof(option), "Option cannot be null");

            Options.Add(option);
            return this;
        }

        public SlashCommandOptionBuilder AddChoice(string Name, int Value)
        {
            if (Choices == null)
                Choices = new List<ApplicationCommandOptionChoiceProperties>();

            if (Choices.Count >= MaxChoiceCount)
                throw new ArgumentOutOfRangeException(nameof(Choices), $"Cannot add more than {MaxChoiceCount} choices!");

            Choices.Add(new ApplicationCommandOptionChoiceProperties()
            {
                Name = Name,
                Value = Value
            });

            return this;
        }
        public SlashCommandOptionBuilder AddChoice(string Name, string Value)
        {
            if (Choices == null)
                Choices = new List<ApplicationCommandOptionChoiceProperties>();

            if (Choices.Count >= MaxChoiceCount)
                throw new ArgumentOutOfRangeException(nameof(Choices), $"Cannot add more than {MaxChoiceCount} choices!");

            Choices.Add(new ApplicationCommandOptionChoiceProperties()
            {
                Name = Name,
                Value = Value
            });

            return this;
        }

        public SlashCommandOptionBuilder WithName(string Name)
        {
            this.Name = Name;
            return this;
        }

        public SlashCommandOptionBuilder WithDescription(string Description)
        {
            this.Description = Description;
            return this;
        }

        public SlashCommandOptionBuilder WithRequired(bool value)
        {
            this.Required = value;
            return this;
        }

        public SlashCommandOptionBuilder WithDefault(bool value)
        {
            this.Default = value;
            return this;
        }
        public SlashCommandOptionBuilder WithType(ApplicationCommandOptionType Type)
        {
            this.Type = Type;
            return this;
        }
    }
}
