using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API
{
    internal class ApplicationCommandOption
    {
        [JsonProperty("type")]
        public ApplicationCommandOptionType Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("default")]
        public Optional<bool> Default { get; set; }

        [JsonProperty("required")]
        public Optional<bool> Required { get; set; }

        [JsonProperty("choices")]
        public Optional<ApplicationCommandOptionChoice[]> Choices { get; set; }

        [JsonProperty("options")]
        public Optional<ApplicationCommandOption[]> Options { get; set; }

        public ApplicationCommandOption() { }

        public ApplicationCommandOption(IApplicationCommandOption cmd)
        {
            this.Choices = cmd.Choices.Select(x => new ApplicationCommandOptionChoice()
            {
                Name = x.Name,
                Value = x.Value
            }).ToArray();

            this.Options = cmd.Options.Select(x => new ApplicationCommandOption(x)).ToArray();

            this.Required = cmd.Required.HasValue
                ? cmd.Required.Value
                : Optional<bool>.Unspecified;
            this.Default = cmd.Default.HasValue
                ? cmd.Default.Value
                : Optional<bool>.Unspecified;

            this.Name = cmd.Name;
            this.Type = cmd.Type;
            this.Description = cmd.Description;
        }
        public ApplicationCommandOption(Discord.ApplicationCommandOptionProperties option)
        {
            this.Choices = option.Choices != null
                ? option.Choices.Select(x => new ApplicationCommandOptionChoice()
                {
                    Name = x.Name,
                    Value = x.Value
                }).ToArray()
                : Optional<ApplicationCommandOptionChoice[]>.Unspecified;

            this.Options = option.Options != null
                ? option.Options.Select(x => new ApplicationCommandOption(x)).ToArray()
                : Optional<ApplicationCommandOption[]>.Unspecified;

            this.Required = option.Required.HasValue
                ? option.Required.Value
                : Optional<bool>.Unspecified;
            this.Default = option.Default.HasValue
                ? option.Default.Value
                : Optional<bool>.Unspecified;

            this.Name = option.Name;
            this.Type = option.Type;
            this.Description = option.Description;
        }
    }
}
