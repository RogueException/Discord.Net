using Discord.Commands;
using JetBrains.Annotations;
using System;
using System.Threading.Tasks;

namespace Discord.Net.Examples.Commands
{
    [PublicAPI]
    internal class CommandServiceExamples
    {
        #region AddEntityTypeReader

        public void AddCustomUserEntityReader(CommandService commandService)
        {
            commandService.AddEntityTypeReader<IUser>(typeof(MyUserTypeReader<>));
        }

        public class MyUserTypeReader<T> : TypeReader
            where T : class, IUser
        {
            public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
            {
                if (ulong.TryParse(input, out var id))
                    return ((await context.Client.GetUserAsync(id)) is T user)
                        ? TypeReaderResult.FromSuccess(user)
                        : TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.");
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Couldn't parse input to ulong.");
            }
        }

        #endregion

        #region AddEntityTypeReader2

        public void AddCustomChannelEntityReader(CommandService commandService)
        {
            commandService.AddEntityTypeReader<IUser>(typeof(MyUserTypeReader<>));
        }

        public class MyChannelTypeReader<T> : TypeReader
            where T : class, IChannel
        {
            public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
            {
                if (ulong.TryParse(input, out var id))
                    return ((await context.Client.GetChannelAsync(id)) is T channel)
                        ? TypeReaderResult.FromSuccess(channel)
                        : TypeReaderResult.FromError(CommandError.ObjectNotFound, "Channel not found.");
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Couldn't parse input to ulong.");
            }
        }

        #endregion
    }
}
