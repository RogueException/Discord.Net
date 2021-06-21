using System;

namespace Discord.Commands
{
    /// <summary>
    ///     Marks the module as a command group.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GroupAttribute : Attribute
    {
        /// <summary>
        ///     Gets the prefix set for the module.
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        ///     Uses this module as the parent.
        ///     <para>Useful when group modules are large and nesting them within the parent class becomes difficult to maintain.</para>
        /// </summary>
        public Type ParentModule { get; set; }

        /// <inheritdoc />
        public GroupAttribute()
        {
            Prefix = null;
        }
        /// <summary>
        ///     Initializes a new <see cref="GroupAttribute" /> with the provided prefix.
        /// </summary>
        /// <param name="prefix">The prefix of the module group.</param>
        public GroupAttribute(string prefix)
        {
            Prefix = prefix;
        }
    }
}
