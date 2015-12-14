#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ConnectorInfo.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop;
	using Ecng.Localization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Information about connection.
	/// </summary>
	public class ConnectorInfo
	{
		/// <summary>
		/// The connection name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The connection description.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// The connection description.
		/// </summary>
		public string Category { get; private set; }

		/// <summary>
		/// The target audience.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Platform.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// The type of adapter.
		/// </summary>
		public Type AdapterType { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectorInfo"/>.
		/// </summary>
		/// <param name="adapterType">The type of transaction or market data adapter.</param>
		public ConnectorInfo(Type adapterType)
		{
			if (adapterType == null)
				throw new ArgumentNullException(nameof(adapterType));

			if (!typeof(IMessageAdapter).IsAssignableFrom(adapterType))
				throw new ArgumentException("adapterType");

			AdapterType = adapterType;
			Name = adapterType.GetDisplayName();
			Description = adapterType.GetDescription();
			Category = adapterType.GetCategory(LocalizedStrings.Str1559);

			var targetPlatform = adapterType.GetAttribute<TargetPlatformAttribute>();
			if (targetPlatform != null)
			{
				PreferLanguage = targetPlatform.PreferLanguage;
				Platform = targetPlatform.Platform;
			}
			else
			{
				PreferLanguage = Languages.English;
				Platform = Platforms.AnyCPU;
			}
		}
	}
}