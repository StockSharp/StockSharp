#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StrategyRunner.StrategyRunnerPublic
File: DiagramStrategyEx.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.StrategyRunner
{
	using System.ComponentModel;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Localization;
	using StockSharp.Xaml.Diagram;

	class DiagramStrategyEx : DiagramStrategy
	{
		/// <summary>
		/// Возвращает <see langword="true"/>, если свойство необходимо отображать в настройках.
		/// </summary>
		/// <param name="pd">Описание свойства.</param>
		/// <returns><see langword="true"/>, если необходимо показывать свойство, иначе <see langword="false"/>.</returns>
		protected override bool NeedShowProperty(PropertyDescriptor pd)
		{
			return pd.Category != LocalizedStrings.Str436 
				&& pd.Category != LocalizedStrings.Str1559 
				&& pd.Category != LocalizedStrings.Str3050 
				&& pd.Category != "Экспорт";
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			if (Security != null)
				storage.SetValue("Security", Security.Id);

			if (Portfolio != null)
				storage.SetValue("Portfolio", Portfolio.Name);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			var portfolio = storage.GetValue<string>("Portfolio");
			if (!portfolio.IsEmpty())
				Portfolio = ConfigManager.GetService<StrategyConnector>().LookupPortfolio(portfolio);

			var security = storage.GetValue<string>("Security");
			if (!security.IsEmpty())
				Security = ConfigManager.GetService<StrategyConnector>().LookupById(security);
		}
	}
}
