using System;

namespace Discord.Commands
{
    /// <summary> Prevents the module from being loaded automatically. </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DontAutoLoadAttribute : Attribute
    {
    }
}
