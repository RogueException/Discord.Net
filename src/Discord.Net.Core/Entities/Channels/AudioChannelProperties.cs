namespace Discord
{
    /// <summary>
    ///     Provides properties that are used to modify an <see cref="IAudioChannel" /> with the specified changes.
    /// </summary>
    public class AudioChannelProperties
    {
        /// <summary>
        ///     Sets whether the user should be muted.
        /// </summary>
        public Optional<bool> SelfMute { get; set; }

        /// <summary>
        ///     Sets whether the user should be deafened.
        /// </summary>
        public Optional<bool> SelfDeaf { get; set; }
    }
}
