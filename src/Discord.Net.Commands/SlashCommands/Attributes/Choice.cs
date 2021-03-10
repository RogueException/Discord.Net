using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    /// <summary>
    ///     Defines the parameter as a choice.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public class Choice : Attribute
    {
        /// <summary>
        ///     The internal value of this choice.
        /// </summary>
        public string choiceStringValue;

        /// <summary>
        ///     The internal value of this choice.
        /// </summary>
        public int? choiceIntValue = null;

        /// <summary>
        ///     The display value of this choice.
        /// </summary>
        public string choiceName;

        public Choice(string choiceName, string choiceValue)
        {
            this.choiceName = choiceName;
            this.choiceStringValue = choiceValue;
        }
        public Choice(string choiceName, int choiceValue)
        {
            this.choiceName = choiceName;
            this.choiceIntValue = choiceValue;
        }
    }
}
