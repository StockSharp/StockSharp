namespace StockSharp.Hydra.Oanda
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.History;
	using StockSharp.Algo.History.Forex;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Forex)]
	[TaskDisplayName(_sourceName)]
	class OandaHistoryTask : BaseHydraTask, ISecurityDownloader
	{
		private const string _sourceName = "OANDA (история)";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class OandaHistorySettings : HydraTaskSettings
		{
			public OandaHistorySettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int Offset
			{
				get { return ExtensionInfo["Offset"].To<int>(); }
				set { ExtensionInfo["Offset"] = value; }
			}
		}

		private OandaHistorySettings _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new OandaHistorySettings(settings);

			if (settings.IsDefault)
			{
				_settings.Offset = 1;
				_settings.StartFrom = new DateTime(1990, 1, 1);
				_settings.Interval = TimeSpan.FromDays(1);
			}
		}

		public override Uri Icon
		{
			get { return "oanda_logo.png".GetResourceUrl(GetType()); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str3807Params.Put(LocalizedStrings.Str3837); }
		}

		public override TaskTypes Type
		{
			get { return TaskTypes.Source; }
		}

		private readonly Type[] _supportedMarketDataTypes = { typeof(Level1ChangeMessage) };

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		protected override TimeSpan OnProcess()
		{
			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			if (this.GetAllSecurity() != null)
			{
				//throw new InvalidOperationException("Источник не поддерживает закачку данных по всем инструментам.");
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			var source = new OandaHistorySource();

			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.Offset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			foreach (var security in GetWorkingSecurities())
			{
				if (!CanProcess())
					break;

				if (!security.MarketDataTypesSet.Contains(typeof(Level1ChangeMessage)))
					break;

				var storage = StorageRegistry.GetLevel1MessageStorage(security.Security, _settings.Drive, _settings.StorageFormat);
				var emptyDates = allDates.Except(storage.Dates).ToArray();

				foreach (var emptyDate in emptyDates)
				{
					if (!CanProcess())
						break;

					try
					{
						this.AddInfoLog(LocalizedStrings.Str3838Params, emptyDate.ToShortDateString(), security.Security.Id);
						var rates = source.LoadRates(security.Security, emptyDate, emptyDate);
						SaveLevel1Changes(security, rates);
					}
					catch (Exception ex)
					{
						HandleError(new InvalidOperationException(LocalizedStrings.Str3839Params
							.Put(emptyDate.ToShortDateString(), security.Security.Id), ex));
					}
				}
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			new OandaHistorySource().Refresh(storage, criteria, newSecurity, isCancelled);
		}
	}
}
