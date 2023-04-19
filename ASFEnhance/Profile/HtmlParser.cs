using AngleSharp.Dom;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Web.Responses;
using System.Text;
using System.Text.RegularExpressions;

namespace ASFEnhance.Profile;

internal static partial class HtmlParser
{

    /// <summary>
    /// 解析个人资料页
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    internal static string? ParseProfilePage(HtmlDocumentResponse? response)
    {
        if (response?.Content == null)
        {
            return null;
        }

        IDocument content = response.Content;

        var eleNickName = content.SelectSingleNode("//div[@class='persona_name']/span[1]");
        string nickName = eleNickName?.TextContent ?? "";

        var eleLevel = content.SelectSingleNode("//div[@class='profile_header_badgeinfo_badge_area']//span[@class='friendPlayerLevelNum']");
        string strLevel = eleLevel?.TextContent ?? "0";

        var eleOnline = content.SelectSingleNode("//div[@class='profile_in_game_name']");
        bool online = eleOnline == null;

        var eleBadgesCount = content.SelectSingleNode("//a[contains(@href,'/badges/')]/span[last()]");
        string? strBadgesCount = eleBadgesCount?.TextContent.Replace(",", "");

        var eleGamesCount = content.SelectSingleNode("//a[contains(@href,'/games/')]/span[last()]");
        string? strGamesCount = eleGamesCount?.TextContent.Trim().Replace(",", "");

        var eleScreenshotsCount = content.SelectSingleNode("//a[contains(@href,'/screenshots/')]/span[last()]");
        string? strScreenshotsCount = eleScreenshotsCount?.TextContent.Replace(",", "");

        var eleVideosCount = content.SelectSingleNode("//a[contains(@href,'/videos/')]/span[last()]");
        string? strVideosCount = eleVideosCount?.TextContent.Replace(",", "");

        var eleWorkshopCount = content.SelectSingleNode("//a[contains(@href,'/myworkshopfiles/')]/span[last()]");
        string? strWorkshopCount = eleWorkshopCount?.TextContent.Replace(",", "");

        var eleRecommendedCount = content.SelectSingleNode("//a[contains(@href,'/recommended/')]/span[last()]");
        string? strRecommendedCount = eleRecommendedCount?.TextContent.Replace(",", "");

        var eleGuideCount = content.SelectSingleNode("//a[contains(@href,'section=guides')]/span[last()]");
        string? strGuideCount = eleGuideCount?.TextContent.Replace(",", "");

        var eleImagesCount = content.SelectSingleNode("//a[contains(@href,'/images/')]/span[last()]");
        string? strImagesCount = eleImagesCount?.TextContent.Replace(",", "");

        var eleGroupsCount = content.SelectSingleNode("//a[contains(@href,'/groups/')]/span[last()]");
        string? strGroupsCount = eleGroupsCount?.TextContent.Replace(",", "");

        var eleFriendsCount = content.SelectSingleNode("//a[contains(@href,'/friends/')]/span[last()]");
        string? strFriendsCount = eleFriendsCount?.TextContent.Replace(",", "");

        StringBuilder result = new();
        result.AppendLine(Langs.MultipleLineResult);
        result.AppendLine(Langs.ProfileHeader);
        result.AppendLine(string.Format(Langs.ProfileNickname, nickName));
        result.AppendLine(string.Format(Langs.ProfileState, online ? Langs.Online : Langs.Offline));

        uint maxFriend = 0;
        if (uint.TryParse(strLevel, out uint level))
        {
            maxFriend = 5 * level + 250;
            result.AppendLine(string.Format(Langs.ProfileLevel, level));
        }

        if (uint.TryParse(strBadgesCount, out uint badges))
        {
            result.AppendLine(string.Format(Langs.ProfileBadges, badges));
        }

        if (uint.TryParse(strGamesCount, out uint games))
        {
            result.AppendLine(string.Format(Langs.ProfileGames, games));
        }

        if (uint.TryParse(strScreenshotsCount, out uint screenshots))
        {
            result.AppendLine(string.Format(Langs.ProfileScreenshots, screenshots));
        }

        if (uint.TryParse(strVideosCount, out uint videos))
        {
            result.AppendLine(string.Format(Langs.ProfileVideos, videos));
        }

        if (uint.TryParse(strWorkshopCount, out uint workshops))
        {
            result.AppendLine(string.Format(Langs.ProfileWorkshop, workshops));
        }

        if (uint.TryParse(strRecommendedCount, out uint recommendeds))
        {
            result.AppendLine(string.Format(Langs.ProfileRecommended, recommendeds));
        }

        if (uint.TryParse(strGuideCount, out uint guides))
        {
            result.AppendLine(string.Format(Langs.ProfileGuide, guides));
        }

        if (uint.TryParse(strImagesCount, out uint images))
        {
            result.AppendLine(string.Format(Langs.ProfileImages, images));
        }

        if (uint.TryParse(strGroupsCount, out uint groups))
        {
            result.AppendLine(string.Format(Langs.ProfileGroups, groups));
        }

        if (uint.TryParse(strFriendsCount, out uint friends))
        {
            result.AppendLine(string.Format(Langs.ProfileFriends, friends, maxFriend > 0 ? maxFriend : "-"));
        }

        return result.ToString();
    }

    /// <summary>
    /// 解析交易设置页
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    internal static string? ParseTradeofferPrivacyPage(HtmlDocumentResponse? response)
    {
        if (response?.Content == null)
        {
            return null;
        }

        var inputEle = response.Content.SelectSingleNode<IElement>("//input[@id='trade_offer_access_url']");

        string? tradeLink = inputEle?.GetAttribute("value");
        return tradeLink;
    }

    [GeneratedRegex("ogg\\/(\\d+)")]
    private static partial Regex MatchGameId();

    /// <summary>
    /// 解析游戏头像GameIds
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    internal static List<int>? ParseAvatarsPageToGameIds(HtmlDocumentResponse? response)
    {
        if (response?.Content == null)
        {
            return null;
        }

        var avatarViewAllEles = response.Content.SelectNodes<IElement>("//div[@id='avatarViewAll']/a");

        if (avatarViewAllEles == null)
        {
            return null;
        }

        List<int> result = new();

        Regex matchGameId = MatchGameId();

        foreach (var avatarViewEle in avatarViewAllEles)
        {
            string? url = avatarViewEle.GetAttribute("href") ?? "";
            Match match = matchGameId.Match(url);

            if (match.Success && int.TryParse(match.Groups[1].Value, out int gameId))
            {
                result.Add(gameId);
            }
        }
        return result;
    }

    /// <summary>
    /// 解析单个游戏的AvatarIds
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    internal static List<int>? ParseSingleGameToAvatarIds(HtmlDocumentResponse? response)
    {
        if (response?.Content == null)
        {
            return null;
        }

        var avatarBucketEles = response.Content.SelectNodes<IElement>("//div[@class='avatarBucket']/div/a");

        if (avatarBucketEles == null)
        {
            return null;
        }

        List<int> result = new();
        foreach (var avatarEle in avatarBucketEles)
        {
            var imgUrl = avatarEle.GetAttribute("href") ?? "";
            string[] items = imgUrl.Split("/");

            if (items.Length > 0)
            {
                if (int.TryParse(items.Last(), out int avatarId))
                {
                    result.Add(avatarId);
                }
            }
        }
        return result;
    }

    [GeneratedRegex("(\\d)[^,]*,")]
    private static partial Regex MatchLevel();

    [GeneratedRegex("gamecards\\/(\\d+)")]
    private static partial Regex MatchBadgeAppId();

    /// <summary>
    /// 解析徽章页信息
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    internal static IDictionary<uint, uint>? ParseCraftableBadgeDict(HtmlDocumentResponse? response)
    {
        if (response?.Content == null)
        {
            return null;
        }

        var badgeEles = response.Content.QuerySelectorAll<IElement>("div.badge_row.is_link");

        if (badgeEles == null)
        {
            return null;
        }

        Dictionary<uint, uint> result = new();
        var regAppId = MatchBadgeAppId();
        var regLevel = MatchLevel();
        foreach (var badgeEle in badgeEles)
        {
            var craftableEle = badgeEle.QuerySelector("a.badge_craft_button");
            var href = craftableEle?.GetAttribute("href");
            if (!string.IsNullOrEmpty(href))
            {
                uint level = 0;
                var match = regAppId.Match(href);
                if (!match.Success || !uint.TryParse(match.Groups[1].Value, out var appid))
                {
                    continue;
                }

                var levelEle = badgeEle.QuerySelector("div.badge_info_description>div:nth-child(2)");
                var desc = levelEle?.TextContent;
                if (!string.IsNullOrEmpty(desc))
                {
                    match = regLevel.Match(desc);
                    if (!match.Success || !uint.TryParse(match.Groups[1].Value, out level))
                    {
                        level = 0;
                    }
                }
                result.Add(appid, level);
            }
        }
        return result;
    }

    /// <summary>
    /// 解析添加好友链接前缀
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    internal static string? ParseInviteLinkPrefix(HtmlDocumentResponse? response)
    {
        if (response?.Content == null)
        {
            return null;
        }

        var configEle = response.Content.QuerySelector<IElement>("#application_config");

        string? configJson = configEle?.GetAttribute("data-userinfo");

        if (string.IsNullOrEmpty(configJson))
        {
            return null;
        }

        var match = MatchShortLink().Match(configJson);
        return match.Success ? match.Groups[1].Value.Replace("\\/", "/") : null;
    }

    [GeneratedRegex("\"short_url\":\"([^\"]+)\"")]
    private static partial Regex MatchShortLink();
}
