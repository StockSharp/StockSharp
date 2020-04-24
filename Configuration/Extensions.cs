#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Configuration.ConfigurationPublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Configuration
{
	using System;
	using System.IO;
	using System.Linq;

	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		//private static readonly Type[] _customCandles = ArrayHelper.Empty<Type>();

		//static Extensions()
		//{
		//	var section = RootSection;

		//	if (section == null)
		//		return;

		//	_customIndicators = SafeAdd<IndicatorElement, IndicatorType>(section.CustomIndicators, elem => new IndicatorType(elem.Type.To<Type>(), elem.Painter.To<Type>()));
		//	_customCandles = SafeAdd<CandleElement, Type>(section.CustomCandles, elem => elem.Type.To<Type>());
		//}

		/// <summary>
		/// Instance of the root section <see cref="StockSharpSection"/>.
		/// </summary>
		public static StockSharpSection RootSection => ConfigManager.InnerConfig.Sections.OfType<StockSharpSection>().FirstOrDefault();

		//private static Type[] _candles;

		///// <summary>
		///// Get all candles.
		///// </summary>
		///// <returns>All candles.</returns>
		//public static IEnumerable<Type> GetCandles()
		//{
		//	return _candles ?? (_candles = typeof(Candle).Assembly
		//		.GetTypes()
		//		.Where(t => !t.IsAbstract && t.IsCandle())
		//		.Concat(_customCandles)
		//		.ToArray());
		//}

		private const string _credentialsFile = "credentials.xml";

		/// <summary>
		/// Try load credentials from <see cref="Paths.CompanyPath"/>.
		/// </summary>
		/// <param name="credentials">The class that contains a login and password to access the services https://stocksharp.com .</param>
		/// <returns><see langword="true"/> if the specified credentials was loaded successfully, otherwise, <see langword="false"/>.</returns>
		public static bool TryLoadCredentials(this ServerCredentials credentials)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			var file = Path.Combine(Paths.CompanyPath, _credentialsFile);

			if (!File.Exists(file))
				return false;

			credentials.Load(new XmlSerializer<SettingsStorage>().Deserialize(file));
			return true;
		}

		/// <summary>
		/// Save the credentials to <see cref="Paths.CompanyPath"/>.
		/// </summary>
		/// <param name="credentials">The class that contains a login and password to access the services https://stocksharp.com .</param>
		public static void SaveCredentials(this ServerCredentials credentials)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			credentials.SaveCredentials(credentials.AutoLogon);
		}

		/// <summary>
		/// Save the credentials to <see cref="Paths.CompanyPath"/>.
		/// </summary>
		/// <param name="credentials">The class that contains a login and password to access the services https://stocksharp.com .</param>
		/// <param name="savePassword">Save password.</param>
		public static void SaveCredentials(this ServerCredentials credentials, bool savePassword)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			var clone = credentials;

			if (!savePassword)
				clone.Password = null;

			Directory.CreateDirectory(Paths.CompanyPath);

			var file = Path.Combine(Paths.CompanyPath, _credentialsFile);

			new XmlSerializer<SettingsStorage>().Serialize(clone.Save(), file);
		}
	}
}