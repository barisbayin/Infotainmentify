using Application.Contracts.UserSocialChannel;
using Core.Entity.User;
using System.Text.Json;

namespace Application.Mappers
{
    public static class UserSocialChannelMapper
    {
        public static UserSocialChannelListDto ToListDto(this UserSocialChannel sc) => new()
        {
            Id = sc.Id,
            ChannelType = sc.ChannelType,
            ChannelName = sc.ChannelName,
            ChannelHandle = sc.ChannelHandle,
            ChannelUrl = sc.ChannelUrl
        };

        public static UserSocialChannelDetailDto ToDetailDto(this UserSocialChannel sc, Dictionary<string, object> tokens = null) => new()
        {
            Id = sc.Id,
            ChannelType = sc.ChannelType,
            ChannelName = sc.ChannelName,
            ChannelHandle = sc.ChannelHandle,
            ChannelUrl = sc.ChannelUrl,
            PlatformChannelId = sc.PlatformChannelId,
            Tokens = tokens,
            TokenExpiresAt = sc.TokenExpiresAt,
            Scopes = sc.Scopes
        };
    }

}
