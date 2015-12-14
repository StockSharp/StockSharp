#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: TargetPlatformAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;

	using Ecng.Interop;
	using Ecng.Localization;

	/// <summary>
	/// Features.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TargetPlatformAttribute : Attribute
	{
		/// <summary>
		/// The target audience.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Platform.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TargetPlatformAttribute"/>.
		/// </summary>
		/// <param name="preferLanguage">The target audience.</param>
		/// <param name="platform">Platform.</param>
		public TargetPlatformAttribute(Languages preferLanguage = Languages.English, Platforms platform = Platforms.AnyCPU)
		{
			PreferLanguage = preferLanguage;
			Platform = platform;
		}
	}
}