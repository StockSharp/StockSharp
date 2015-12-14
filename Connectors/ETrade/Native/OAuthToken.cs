#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Native.ETrade
File: OAuthToken.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade.Native
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	
	/// <summary>
	/// Token. Uses in OAuth 1.0a.
	/// </summary>
	public class OAuthToken
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthToken"/> class.
		/// </summary>
		/// <param name="consumerKey">Consumer key.</param>
		/// <param name="token">Token.</param>
		/// <param name="secret">Secret.</param>
		public OAuthToken(string consumerKey, string token, string secret)
		{
			ConsumerKey = consumerKey;
			Token = token;
			Secret = secret;
		}

		/// <summary>
		/// Consumer key.
		/// </summary>
		public string ConsumerKey {get; private set;}

		/// <summary>
		/// Token.
		/// </summary>
		public string Token { get; private set; }

		/// <summary>
		/// Secret.
		/// </summary>
		public string Secret { get; private set; }

		/// <summary>
		/// Token serialization.
		/// </summary>
		/// <returns>Serialized as string the token.</returns>
		public string Serialize()
		{
			return "{0}\n{1}\n{2}".Put(ConsumerKey, Token, Secret);
		}

		/// <summary>
		/// Toke deserialization.
		/// </summary>
		/// <returns>Token.</returns>
		public static OAuthToken Deserialize(string serialized)
		{
			if(serialized.IsEmpty()) return null;

			var arr = serialized.Split('\n');

			if(arr.Length != 3) throw new ArgumentException(LocalizedStrings.Str3372, nameof(serialized));

			return new OAuthToken(arr[0], arr[1], arr[2]);
		}
	}
}