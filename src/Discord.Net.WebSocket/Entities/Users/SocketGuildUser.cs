﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord.Audio;
using Discord.Rest;
using UserModel = Discord.API.User;
using MemberModel = Discord.API.GuildMember;
using PresenceModel = Discord.API.Presence;

namespace Discord.WebSocket
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class SocketGuildUser : SocketUser, IGuildUser
    {
        private long? _joinedAtTicks;
        private ImmutableArray<ulong> _roleIds;

        internal SocketGuildUser(SocketGuild guild, SocketGlobalUser globalUser)
            : base(guild.Discord, globalUser.Id)
        {
            Guild = guild;
            GlobalUser = globalUser;
        }

        internal override SocketGlobalUser GlobalUser { get; }
        public SocketGuild Guild { get; }
        internal override SocketPresence Presence { get; set; }

        public IReadOnlyCollection<SocketRole> Roles
            => _roleIds.Select(id => Guild.GetRole(id)).Where(x => x != null)
                .ToReadOnlyCollection(() => _roleIds.Length);

        public SocketVoiceChannel VoiceChannel => VoiceState?.VoiceChannel;
        public SocketVoiceState? VoiceState => Guild.GetVoiceState(Id);
        public AudioInStream AudioStream => Guild.GetAudioStream(Id);

        /// <summary> The position of the user within the role hierarchy. </summary>
        /// <remarks>
        ///     The returned value equal to the position of the highest role the user has,
        ///     or int.MaxValue if user is the server owner.
        /// </remarks>
        public int Hierarchy
        {
            get
            {
                if (Guild.OwnerId == Id)
                    return int.MaxValue;

                var maxPos = 0;
                foreach (var t in _roleIds)
                {
                    var role = Guild.GetRole(t);
                    if (role != null && role.Position > maxPos)
                        maxPos = role.Position;
                }

                return maxPos;
            }
        }

        public string Nickname { get; private set; }

        public override bool IsBot
        {
            get => GlobalUser.IsBot;
            internal set => GlobalUser.IsBot = value;
        }

        public override string Username
        {
            get => GlobalUser.Username;
            internal set => GlobalUser.Username = value;
        }

        public override ushort DiscriminatorValue
        {
            get => GlobalUser.DiscriminatorValue;
            internal set => GlobalUser.DiscriminatorValue = value;
        }

        public override string AvatarId
        {
            get => GlobalUser.AvatarId;
            internal set => GlobalUser.AvatarId = value;
        }

        public GuildPermissions GuildPermissions => new GuildPermissions(Permissions.ResolveGuild(Guild, this));

        public override bool IsWebhook => false;
        public bool IsSelfDeafened => VoiceState?.IsSelfDeafened ?? false;
        public bool IsSelfMuted => VoiceState?.IsSelfMuted ?? false;
        public bool IsSuppressed => VoiceState?.IsSuppressed ?? false;
        public bool IsDeafened => VoiceState?.IsDeafened ?? false;
        public bool IsMuted => VoiceState?.IsMuted ?? false;
        public DateTimeOffset? JoinedAt => DateTimeUtils.FromTicks(_joinedAtTicks);
        public string VoiceSessionId => VoiceState?.VoiceSessionId ?? "";

        public Task ModifyAsync(Action<GuildUserProperties> func, RequestOptions options = null)
            => UserHelper.ModifyAsync(this, Discord, func, options);

        public Task KickAsync(string reason = null, RequestOptions options = null)
            => UserHelper.KickAsync(this, Discord, reason, options);

        /// <inheritdoc />
        public Task AddRoleAsync(IRole role, RequestOptions options = null)
            => AddRolesAsync(new[] {role}, options);

        /// <inheritdoc />
        public Task AddRolesAsync(IEnumerable<IRole> roles, RequestOptions options = null)
            => UserHelper.AddRolesAsync(this, Discord, roles, options);

        /// <inheritdoc />
        public Task RemoveRoleAsync(IRole role, RequestOptions options = null)
            => RemoveRolesAsync(new[] {role}, options);

        /// <inheritdoc />
        public Task RemoveRolesAsync(IEnumerable<IRole> roles, RequestOptions options = null)
            => UserHelper.RemoveRolesAsync(this, Discord, roles, options);

        public ChannelPermissions GetPermissions(IGuildChannel channel)
            => new ChannelPermissions(Permissions.ResolveChannel(Guild, this, channel, GuildPermissions.RawValue));

        //IGuildUser
        IGuild IGuildUser.Guild => Guild;
        ulong IGuildUser.GuildId => Guild.Id;
        IReadOnlyCollection<ulong> IGuildUser.RoleIds => _roleIds;

        //IVoiceState
        IVoiceChannel IVoiceState.VoiceChannel => VoiceChannel;

        internal static SocketGuildUser Create(SocketGuild guild, ClientState state, UserModel model)
        {
            var entity = new SocketGuildUser(guild, guild.Discord.GetOrCreateUser(state, model));
            entity.Update(state, model);
            entity.UpdateRoles(new ulong[0]);
            return entity;
        }

        internal static SocketGuildUser Create(SocketGuild guild, ClientState state, MemberModel model)
        {
            var entity = new SocketGuildUser(guild, guild.Discord.GetOrCreateUser(state, model.User));
            entity.Update(state, model);
            return entity;
        }

        internal static SocketGuildUser Create(SocketGuild guild, ClientState state, PresenceModel model)
        {
            var entity = new SocketGuildUser(guild, guild.Discord.GetOrCreateUser(state, model.User));
            entity.Update(state, model, false);
            return entity;
        }

        internal void Update(ClientState state, MemberModel model)
        {
            base.Update(state, model.User);
            if (model.JoinedAt.IsSpecified)
                _joinedAtTicks = model.JoinedAt.Value.UtcTicks;
            if (model.Nick.IsSpecified)
                Nickname = model.Nick.Value;
            if (model.Roles.IsSpecified)
                UpdateRoles(model.Roles.Value);
        }

        internal void Update(ClientState state, PresenceModel model, bool updatePresence)
        {
            if (updatePresence)
            {
                Presence = SocketPresence.Create(model);
                GlobalUser.Update(state, model);
            }

            if (model.Nick.IsSpecified)
                Nickname = model.Nick.Value;
            if (model.Roles.IsSpecified)
                UpdateRoles(model.Roles.Value);
        }

        private void UpdateRoles(ulong[] roleIds)
        {
            var roles = ImmutableArray.CreateBuilder<ulong>(roleIds.Length + 1);
            roles.Add(Guild.Id);
            foreach (var t in roleIds)
                roles.Add(t);

            _roleIds = roles.ToImmutable();
        }

        internal new SocketGuildUser Clone() => MemberwiseClone() as SocketGuildUser;
    }
}
