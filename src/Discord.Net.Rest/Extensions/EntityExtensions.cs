using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.Rest
{
    internal static class EntityExtensions
    {
        public static IEmote ToIEmote(this API.Emoji model)
        {
            if (model.Id.HasValue)
                return model.ToEntity();
            return new Emoji(model.Name);
        }

        public static GuildEmote ToEntity(this API.Emoji model)
            => new GuildEmote(model.Id.Value,
                model.Name,
                model.Animated.GetValueOrDefault(),
                model.Managed,
                model.RequireColons,
                ImmutableArray.Create(model.Roles),
                model.User.IsSpecified ? model.User.Value.Id : (ulong?)null);

        public static Embed ToEntity(this API.Embed model)
        {
            return new Embed(model.Type, model.Title, model.Description, model.Url, model.Timestamp,
                model.Color.HasValue ? new Color(model.Color.Value) : (Color?)null,
                model.Image.IsSpecified ? model.Image.Value.ToEntity() : (EmbedImage?)null,
                model.Video.IsSpecified ? model.Video.Value.ToEntity() : (EmbedVideo?)null,
                model.Author.IsSpecified ? model.Author.Value.ToEntity() : (EmbedAuthor?)null,
                model.Footer.IsSpecified ? model.Footer.Value.ToEntity() : (EmbedFooter?)null,
                model.Provider.IsSpecified ? model.Provider.Value.ToEntity() : (EmbedProvider?)null,
                model.Thumbnail.IsSpecified ? model.Thumbnail.Value.ToEntity() : (EmbedThumbnail?)null,
                model.Fields.IsSpecified ? model.Fields.Value.Select(x => x.ToEntity()).ToImmutableArray() : ImmutableArray.Create<EmbedField>());
        }
        public static RoleTags ToEntity(this API.RoleTags model)
        {
            return new RoleTags(
                model.BotId.IsSpecified ? model.BotId.Value : (ulong?)null,
                model.IntegrationId.IsSpecified ? model.IntegrationId.Value : (ulong?)null,
                model.IsPremiumSubscriber.IsSpecified ? true : false);
        }
        public static API.Embed ToModel(this Embed entity)
        {
            if (entity == null) return null;
            var model = new API.Embed
            {
                Type = entity.Type,
                Title = entity.Title,
                Description = entity.Description,
                Url = entity.Url,
                Timestamp = entity.Timestamp,
                Color = entity.Color?.RawValue
            };
            if (entity.Author != null)
                model.Author = entity.Author.Value.ToModel();
            model.Fields = entity.Fields.Select(x => x.ToModel()).ToArray();
            if (entity.Footer != null)
                model.Footer = entity.Footer.Value.ToModel();
            if (entity.Image != null)
                model.Image = entity.Image.Value.ToModel();
            if (entity.Provider != null)
                model.Provider = entity.Provider.Value.ToModel();
            if (entity.Thumbnail != null)
                model.Thumbnail = entity.Thumbnail.Value.ToModel();
            if (entity.Video != null)
                model.Video = entity.Video.Value.ToModel();
            return model;
        }
        public static API.AllowedMentions ToModel(this AllowedMentions entity)
        {
            return new API.AllowedMentions()
            {
                Parse = entity.AllowedTypes?.EnumerateMentionTypes().ToArray(),
                Roles = entity.RoleIds?.ToArray(),
                Users = entity.UserIds?.ToArray(),
                RepliedUser = entity.MentionRepliedUser ?? Optional.Create<bool>(),
            };
        }
        public static API.MessageReference ToModel(this MessageReference entity)
        {
            return new API.MessageReference()
            {
                ChannelId = entity.InternalChannelId,
                GuildId = entity.GuildId,
                MessageId = entity.MessageId,
            };
        }
        public static IEnumerable<string> EnumerateMentionTypes(this AllowedMentionTypes mentionTypes)
        {
            if (mentionTypes.HasFlag(AllowedMentionTypes.Everyone))
                yield return "everyone";
            if (mentionTypes.HasFlag(AllowedMentionTypes.Roles))
                yield return "roles";
            if (mentionTypes.HasFlag(AllowedMentionTypes.Users))
                yield return "users";
        }
        public static EmbedAuthor ToEntity(this API.EmbedAuthor model)
        {
            return new EmbedAuthor(model.Name, model.Url, model.IconUrl, model.ProxyIconUrl);
        }
        public static API.EmbedAuthor ToModel(this EmbedAuthor entity)
        {
            return new API.EmbedAuthor { Name = entity.Name, Url = entity.Url, IconUrl = entity.IconUrl };
        }
        public static EmbedField ToEntity(this API.EmbedField model)
        {
            return new EmbedField(model.Name, model.Value, model.Inline);
        }
        public static API.EmbedField ToModel(this EmbedField entity)
        {
            return new API.EmbedField { Name = entity.Name, Value = entity.Value, Inline = entity.Inline };
        }
        public static EmbedFooter ToEntity(this API.EmbedFooter model)
        {
            return new EmbedFooter(model.Text, model.IconUrl, model.ProxyIconUrl);
        }
        public static API.EmbedFooter ToModel(this EmbedFooter entity)
        {
            return new API.EmbedFooter { Text = entity.Text, IconUrl = entity.IconUrl };
        }
        public static EmbedImage ToEntity(this API.EmbedImage model)
        {
            return new EmbedImage(model.Url, model.ProxyUrl,
                  model.Height.IsSpecified ? model.Height.Value : (int?)null,
                  model.Width.IsSpecified ? model.Width.Value : (int?)null);
        }
        public static API.EmbedImage ToModel(this EmbedImage entity)
        {
            return new API.EmbedImage { Url = entity.Url };
        }
        public static EmbedProvider ToEntity(this API.EmbedProvider model)
        {
            return new EmbedProvider(model.Name, model.Url);
        }
        public static API.EmbedProvider ToModel(this EmbedProvider entity)
        {
            return new API.EmbedProvider { Name = entity.Name, Url = entity.Url };
        }
        public static EmbedThumbnail ToEntity(this API.EmbedThumbnail model)
        {
            return new EmbedThumbnail(model.Url, model.ProxyUrl,
                  model.Height.IsSpecified ? model.Height.Value : (int?)null,
                  model.Width.IsSpecified ? model.Width.Value : (int?)null);
        }
        public static API.EmbedThumbnail ToModel(this EmbedThumbnail entity)
        {
            return new API.EmbedThumbnail { Url = entity.Url };
        }
        public static EmbedVideo ToEntity(this API.EmbedVideo model)
        {
            return new EmbedVideo(model.Url,
                  model.Height.IsSpecified ? model.Height.Value : (int?)null,
                  model.Width.IsSpecified ? model.Width.Value : (int?)null);
        }
        public static API.EmbedVideo ToModel(this EmbedVideo entity)
        {
            return new API.EmbedVideo { Url = entity.Url };
        }

        public static API.Image ToModel(this Image entity)
        {
            return new API.Image(entity.Stream);
        }

        public static Overwrite ToEntity(this API.Overwrite model)
        {
            return new Overwrite(model.TargetId, model.TargetType, new OverwritePermissions(model.Allow, model.Deny));
        }
    }
}
