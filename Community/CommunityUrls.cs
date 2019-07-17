namespace StockSharp.Community
{
	using StockSharp.Localization;

	/// <summary>
	/// Community urls.
	/// </summary>
	public static class CommunityUrls
	{
		/// <summary>
		/// Get website url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetWebSiteUrl()
		{
			return $"https://stocksharp.{LocalizedStrings.Domain}";
		}

		/// <summary>
		/// Get user url.
		/// </summary>
		/// <param name="userId">User id.</param>
		/// <returns>Localized url.</returns>
		public static string GetUserUrl(long userId)
		{
			return $"{GetWebSiteUrl()}/users/{userId}/";
		}

		/// <summary>
		/// Get strategy url.
		/// </summary>
		/// <param name="robotId">The strategy identifier.</param>
		/// <returns>Localized url.</returns>
		public static string GetRobotLink(long robotId)
		{
			return $"{GetWebSiteUrl()}/robot/{robotId}/";
		}

		/// <summary>
		/// Get file url.
		/// </summary>
		/// <param name="fileId">File ID.</param>
		/// <returns>Localized url.</returns>
		public static string GetFileLink(object fileId)
		{
			return $"{GetWebSiteUrl()}/file/{fileId}/";
		}

		/// <summary>
		/// To create localized url.
		/// </summary>
		/// <param name="docUrl">Help topic.</param>
		/// <returns>Localized url.</returns>
		public static string GetDocUrl(string docUrl)
		{
			return $"https://doc.stocksharp.{LocalizedStrings.Domain}/html/{docUrl}";
		}

		/// <summary>
		/// Get open account url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetOpenAccountUrl()
		{
			return $"{GetWebSiteUrl()}/broker/openaccount/";
		}

		/// <summary>
		/// Get sign up url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetSignUpUrl()
		{
			return $"{GetWebSiteUrl()}/register/";
		}

		/// <summary>
		/// Get forgot password url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetForgotUrl()
		{
			return $"{GetWebSiteUrl()}/forgot/";
		}
	}
}