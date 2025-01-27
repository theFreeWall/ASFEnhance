using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Web.Responses;
using ASFEnhance.Data;

namespace ASFEnhance.Cart;

internal static class WebRequest
{
    /// <summary>
    /// 读取当前购物车
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<CartItemResponse?> GetCartGames(Bot bot)
    {
        var request = new Uri(SteamStoreURL, "/cart/");
        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        return HtmlParser.ParseCartPage(response);
    }

    /// <summary>
    /// 添加到购物车
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="gameId"></param>
    /// <returns></returns>
    internal static async Task<bool?> AddCart(Bot bot, SteamGameId gameId)
    {
        if (gameId.Type == SteamGameIdType.Sub || gameId.Type == SteamGameIdType.Bundle)
        {
            return await AddCart(bot, gameId.GameId, gameId.Type == SteamGameIdType.Bundle).ConfigureAwait(false);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 添加到购物车
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="subId"></param>
    /// <param name="isBundle"></param>
    /// <returns></returns>
    internal static async Task<bool?> AddCart(Bot bot, uint subId, bool isBundle = false)
    {
        string type = isBundle ? "bundle" : "sub";

        var request = new Uri(SteamStoreURL, "/cart/");
        var referer = new Uri(SteamStoreURL, $"/{type}/{subId}");

        var data = new Dictionary<string, string>(5, StringComparer.Ordinal)
        {
            { "action", "add_to_cart" },
            { type + "id", subId.ToString() },
            { "originating_snr", "1_direct-navigation__" },
        };

        var response = await bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(request, data: data, referer: referer).ConfigureAwait(false);

        return response != null;
    }

    /// <summary>
    /// 清空当前购物车
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<bool?> ClearCart(Bot bot)
    {
        var request = new Uri(SteamStoreURL, "/cart/");

        bot.ArchiWebHandler.WebBrowser.CookieContainer.SetCookies(SteamStoreURL, "shoppingCartGID=-1");

        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        var cartResponse = HtmlParser.ParseCartPage(response);

        if (cartResponse == null)
        {
            return null;
        }

        return cartResponse.CartItems.Count == 0;
    }

    /// <summary>
    /// 读取购物车可用区域信息
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> CartGetCountries(Bot bot)
    {
        var request = new Uri(SteamStoreURL, "/cart/");

        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        return HtmlParser.ParseCartCountries(response);
    }

    /// <summary>
    /// 购物车改区
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="countryCode"></param>
    /// <returns></returns>
    internal static async Task<bool> CartSetCountry(Bot bot, string countryCode)
    {
        var request = new Uri(SteamStoreURL, "/account/setcountry");
        var referer = new Uri(SteamStoreURL, "/cart/");

        Dictionary<string, string> data = new(2, StringComparer.Ordinal)
        {
            { "cc", countryCode.ToUpperInvariant() },
        };

        var result = await bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(request, data: data, referer: referer).ConfigureAwait(false);

        if (result?.Content == null)
        {
            return false;
        }

        return result.Content.TextContent == "true";
    }

    /// <summary>
    /// 结算购物车
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="asGift"></param>
    /// <returns></returns>
    internal static async Task<HtmlDocumentResponse?> CheckOut(Bot bot, bool asGift = false)
    {
        var shoppingCartId = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(SteamStoreURL, "shoppingCartGID");

        if (string.IsNullOrEmpty(shoppingCartId) || shoppingCartId == "-1")
        {
            bot.ArchiLogger.LogNullError(nameof(shoppingCartId));
            return null;
        }

        var queries = string.Format("/checkout/?purchasetype={0}&cart={1}&snr=1_8_4__503", asGift ? "gift" : "self", shoppingCartId);

        var request = new Uri(SteamCheckoutURL, queries);
        var referer = new Uri(SteamStoreURL, "/cart/");

        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: referer).ConfigureAwait(false);

        if (response == null)
        {
            bot.ArchiLogger.LogNullError(nameof(response));
            return null;
        }

        return response;
    }

    /// <summary>
    /// 初始化付款
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<PurchaseResponse?> InitTransaction(Bot bot)
    {
        var request = new Uri(SteamCheckoutURL, "/checkout/inittransaction/");
        var referer = new Uri(SteamCheckoutURL, "/checkout/");

        string? shoppingCartId = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(SteamStoreURL, "shoppingCartGID");

        if (string.IsNullOrEmpty(shoppingCartId))
        {
            if (string.IsNullOrEmpty(shoppingCartId))
            {
                bot.ArchiLogger.LogNullError(nameof(shoppingCartId));
                return null;
            }
        }

        var data = new Dictionary<string, string>(4, StringComparer.Ordinal)
        {
            { "gidShoppingCart", shoppingCartId },
            { "gidReplayOfTransID", "-1" },
            { "PaymentMethod", "steamaccount" },
        };

        var response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<PurchaseResponse>(request, data: data, referer: referer).ConfigureAwait(false);

        if (response == null)
        {
            bot.ArchiLogger.LogNullError(nameof(response));
            return null;
        }

        return response?.Content;
    }

    /// <summary>
    /// 取消付款
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="transid"></param>
    /// <returns></returns>
    internal static async Task<ResultResponse?> CancelTransaction(Bot bot, string transid)
    {
        var request = new Uri(SteamCheckoutURL, "/checkout/canceltransaction/");
        var referer = new Uri(SteamCheckoutURL, "/checkout/");

        var data = new Dictionary<string, string>(4, StringComparer.Ordinal)
        {
            { "transid", transid },
        };

        var response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<ResultResponse>(request, data: data, referer: referer).ConfigureAwait(false);

        if (response == null)
        {
            bot.ArchiLogger.LogNullError(nameof(response));
            return null;
        }

        return response?.Content;
    }

    /// <summary>
    /// 初始化付款 (赠送礼物)
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="steamId32"></param>
    /// <returns></returns>
    internal static async Task<PurchaseResponse?> InitTransaction(Bot bot, ulong steamId32)
    {
        var request = new Uri(SteamCheckoutURL, "/checkout/inittransaction/");
        var referer = new Uri(SteamCheckoutURL, "/checkout/");

        string? shoppingCartId = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(new(SteamCheckoutURL, "/checkout/"), "beginCheckoutCart");

        if (string.IsNullOrEmpty(shoppingCartId))
        {
            if (string.IsNullOrEmpty(shoppingCartId))
            {
                bot.ArchiLogger.LogNullError(nameof(shoppingCartId));
                return null;
            }
        }

        var data = new Dictionary<string, string>(11, StringComparer.Ordinal)
        {
            { "gidShoppingCart", shoppingCartId },
            { "gidReplayOfTransID", "-1" },
            { "PaymentMethod", "steamaccount" },
            { "bIsGift", "1" },
            { "GifteeAccountID", steamId32.ToString() },
            { "GifteeEmail", "" },
            { "GifteeName", string.Format( Langs.GifteeName, nameof(ASFEnhance)) },
            { "GiftMessage", string.Format( Langs.GiftMessage, nameof(ASFEnhance), MyVersion.ToString()) },
            { "Sentiment", "祝你好运" },
            { "Signature", string.Format( Langs.GiftSignature, nameof(ASFEnhance)) },
            { "ScheduledSendOnDate", "0" },
        };

        var response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<PurchaseResponse>(request, data: data, referer: referer).ConfigureAwait(false);

        if (response?.Content == null)
        {
            bot.ArchiLogger.LogNullError(nameof(response));
            return null;
        }

        return response.Content;
    }

    /// <summary>
    /// 初始化付款 (赠送礼物)
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    internal static async Task<PurchaseResponse?> InitTransaction(Bot bot, string email)
    {
        var request = new Uri(SteamCheckoutURL, "/checkout/inittransaction/");
        var referer = new Uri(SteamCheckoutURL, "/checkout/");

        string? shoppingCartId = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(new(SteamCheckoutURL, "/checkout/"), "beginCheckoutCart");

        if (string.IsNullOrEmpty(shoppingCartId))
        {
            if (string.IsNullOrEmpty(shoppingCartId))
            {
                bot.ArchiLogger.LogNullError(nameof(shoppingCartId));
                return null;
            }
        }

        var version = MyVersion;

        var data = new Dictionary<string, string>(11, StringComparer.Ordinal)
        {
            { "gidShoppingCart", shoppingCartId },
            { "gidReplayOfTransID", "-1" },
            { "PaymentMethod", "steamaccount" },
            { "bIsGift", "1" },
            { "GifteeAccountID", "" },
            { "GifteeEmail", email },
            { "GifteeName", string.Format( Langs.GifteeName, nameof(ASFEnhance)) },
            { "GiftMessage", string.Format( Langs.GiftMessage, nameof(ASFEnhance), version.Major, version.Minor, version.Build, version.Revision) },
            { "Sentiment", "祝你好运" },
            { "Signature", string.Format( Langs.GiftSignature, nameof(ASFEnhance)) },
            { "ScheduledSendOnDate", "0" },
        };

        var response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<PurchaseResponse>(request, data: data, referer: referer).ConfigureAwait(false);

        if (response?.Content == null)
        {
            bot.ArchiLogger.LogNullError(nameof(response));
            return null;
        }

        return response.Content;
    }

    /// <summary>
    /// 获取购物车总价格
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="TransId"></param>
    /// <param name="asGift"></param>
    /// <returns></returns>
    internal static async Task<FinalPriceResponse?> GetFinalPrice(Bot bot, string TransId, bool asGift = false)
    {
        string? shoppingCartId = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(new(SteamCheckoutURL, "/checkout/"), "beginCheckoutCart");

        if (string.IsNullOrEmpty(shoppingCartId) || shoppingCartId == "-1")
        {
            if (string.IsNullOrEmpty(shoppingCartId))
            {
                bot.ArchiLogger.LogNullError(nameof(shoppingCartId));
                return null;
            }
            else
            {
                bot.ArchiLogger.LogGenericWarning("购物车是空的");
                return null;
            }
        }

        string queries = string.Format("/checkout/getfinalprice/?count=1&transid={0}&purchasetype={1}&microtxnid=-1&cart={2}&gidReplayOfTransID=-1", TransId, asGift ? "gift" : "self", shoppingCartId);

        var request = new Uri(SteamCheckoutURL, queries);
        var referer = new Uri(SteamCheckoutURL, "/checkout/");

        var response = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<FinalPriceResponse>(request, referer: referer).ConfigureAwait(false);

        return response?.Content;
    }

    /// <summary>
    /// 完成付款
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="transId"></param>
    /// <returns></returns>
    internal static async Task<TransactionStatusResponse?> FinalizeTransaction(Bot bot, string transId)
    {
        var request = new Uri(SteamCheckoutURL, "/checkout/finalizetransaction/");
        var referer = new Uri(SteamCheckoutURL, "/checkout/");

        var data = new Dictionary<string, string>(3, StringComparer.Ordinal)
        {
            { "transid", transId },
            { "CardCVV2", "" },
            { "browserInfo", @"{""language"":""zh-CN"",""javaEnabled"":""false"",""colorDepth"":24,""screenHeight"":1080,""screenWidth"":1920}" }
        };

        var response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<FinalizeTransactionResponse>(request, data: data, referer: referer).ConfigureAwait(false);

        string queries = string.Format("/checkout/transactionstatus/?count=1&transid={0}", transId);

        request = new(SteamCheckoutURL, queries);

        var response2 = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<TransactionStatusResponse>(request, referer: referer).ConfigureAwait(false);

        if (response?.Content == null)
        {
            bot.ArchiLogger.LogNullError(nameof(response));
            return null;
        }

        if (response2?.Content == null)
        {
            bot.ArchiLogger.LogNullError(nameof(response2));
            return null;
        }

        return response2.Content;
    }

    /// <summary>
    /// 获取数字礼品卡可用面额
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<List<DigitalGiftCardOption>?> GetDigitalGiftCardOptions(Bot bot)
    {
        var request = new Uri(SteamStoreURL, "/digitalgiftcards/selectgiftcard");
        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        return HtmlParser.ParseDigitalGiftCardOptions(response);
    }

    /// <summary>
    /// 提交礼品卡支付
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    internal static async Task<HtmlDocumentResponse?> SubmitGiftCard(Bot bot, uint amount)
    {
        var request = new Uri(SteamStoreURL, "/digitalgiftcards/submitgiftcard");
        var referer = new Uri(SteamStoreURL, "/digitalgiftcards/selectgiftcard");

        var data = new Dictionary<string, string>(4, StringComparer.Ordinal)
        {
            { "action", "add_to_cart" },
            { "currency", bot.WalletCurrency.ToString() },
            { "amount", amount.ToString() },
        };

        var response = await bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(request, data: data, referer: referer, session: ArchiWebHandler.ESession.CamelCase).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// 初始化付款 (赠送礼物)
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="steamId32"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    internal static async Task<PurchaseResponse?> InitTransactionDigicalCard(Bot bot, ulong steamId32, string method = "alipay")
    {
        var request = new Uri(SteamCheckoutURL, "/checkout/inittransaction/");

        string? shoppingCartId = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(new(SteamCheckoutURL, "/checkout/"), "beginCheckoutCart");

        if (string.IsNullOrEmpty(shoppingCartId))
        {
            if (string.IsNullOrEmpty(shoppingCartId))
            {
                bot.ArchiLogger.LogNullError(nameof(shoppingCartId));
                return null;
            }
        }

        var referer = new Uri(SteamCheckoutURL, $"/checkout?cart={shoppingCartId}&purchasetype=gift");

        var version = MyVersion;

        var data = new Dictionary<string, string>(11, StringComparer.Ordinal)
        {
            { "gidShoppingCart", shoppingCartId },
            { "gidReplayOfTransID", "-1" },
            { "PaymentMethod", method },
            { "abortPendingTransactions", "0" },
            { "bHasCardInfo", "0" },
            { "CardNumber", "" },
            { "CardExpirationYear", "" },
            { "CardExpirationMonth", "" },
            { "FirstName", "" },
            { "LastName", "" },
            { "Address", "" },
            { "AddressTwo", "" },
            { "Country", "CN" },
            { "City", "" },
            { "State", "" },
            { "PostalCode", "" },
            { "Phone", "" },
            { "ShippingFirstName", "" },
            { "ShippingLastName", "" },
            { "ShippingAddress", "" },
            { "ShippingAddressTwo", "" },
            { "ShippingCountry", "CN" },
            { "ShippingCity", "" },
            { "ShippingState", "" },
            { "ShippingPostalCode", "" },
            { "ShippingPhone", "" },
            { "bIsGift", "1" },
            { "GifteeAccountID", steamId32.ToString() },
            { "GifteeEmail", "" },
            { "GifteeName", Langs.GifteeName },
            { "GiftMessage", string.Format( Langs.GiftMessage, nameof(ASFEnhance), version.Major, version.Minor, version.Build, version.Revision) },
            { "Sentiment", "祝你好运" },
            { "Signature", string.Format( Langs.GiftSignature, nameof(ASFEnhance)) },
            { "ScheduledSendOnDate", "0" },
            { "BankAccount", "" },
            { "BankCode", "" },
            { "BankIBAN", "" },
            { "BankBIC", "" },
            { "TPBankID", "" },
            { "BankAccountID", "" },
            { "bSaveBillingAddress", "1" },
            { "gidPaymentID", "" },
            { "bUseRemainingSteamAccount", "0" },
            { "bPreAuthOnly", "0" },
        };

        var response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<PurchaseResponse>(request, data: data, referer: referer).ConfigureAwait(false);

        return response?.Content;
    }

    /// <summary>
    /// 获取外部支付链接
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="transId"></param>
    /// <returns></returns>
    internal static async Task<Uri?> GetExternalPaymentUrl(Bot bot, string transId)
    {
        var request = new Uri(SteamCheckoutURL, $"/checkout/externallink/?transid={transId}");

        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        var payload = HtmlParser.FetchPayload(response);

        if (payload == null)
        {
            return null;
        }

        using var httpClient = bot.ArchiWebHandler.WebBrowser.GenerateDisposableHttpClient(false);

        request = new Uri("https://globalapi.smart2pay.com");

        var x = await httpClient.PostAsync(request, new FormUrlEncodedContent(payload)).ConfigureAwait(false);

        //var response2 = await bot.ArchiWebHandler.WebBrowser.UrlPost(request, data: payload).ConfigureAwait(false);

        return null;
    }
}
