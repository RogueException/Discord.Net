using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API
{
    internal class InteractionFollowupMessage
    {
        public string Content { get; set; }
        public Optional<string> Username { get; set; }
        public Optional<string> AvatarUrl { get; set; }
        public Optional<bool> TTS { get; set; }
        public Optional<Stream> File { get; set; }
        public Embed[] Embeds { get; set; }

    }
}
