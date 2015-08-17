namespace StockSharp.Hydra.RtsCompetition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;

	using StockSharp.Algo;
	using StockSharp.Algo.History.Russian.Rts;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2826Key)]
	[TargetPlatform(Languages.Russian)]
	[TaskDoc("http://stocksharp.com/doc/html/53c15f3a-4320-422d-850a-22ef6c90d76b.htm")]
	[TaskIcon("rts_competition_logo.png")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.History |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.OrderLog)]
	class RtsCompetitionTask : BaseHydraTask
	{
		private const string _sourceName = "ЛЧИ";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class RtsCompetitionSettings : HydraTaskSettings
		{
			public RtsCompetitionSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("IgnoreWeekends", true);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2283Key)]
			[PropertyOrder(0)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(1)]
			public int Offset
			{
				get { return ExtensionInfo["RtsOffset"].To<int>(); }
				set { ExtensionInfo["RtsOffset"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(2)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo["IgnoreWeekends"]; }
				set { ExtensionInfo["IgnoreWeekends"] = value; }
			}
		}

		private RtsCompetitionSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new RtsCompetitionSettings(settings);

			if (settings.IsDefault)
			{
				_settings.StartFrom = new DateTime(2006, 09, 15);
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.Offset = 1;
				_settings.IgnoreWeekends = true;
			}
		}

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return new[] { typeof(OrderLogItem) }; }
		}

		protected override TimeSpan OnProcess()
		{
			var competition = new Competition();

			var offset = TimeSpan.FromDays(_settings.Offset);

			if (Competition.AllYears.Any(y => y.Year == _settings.StartFrom.Year) && (_settings.StartFrom + offset) < DateTime.Today)
			{
				this.AddInfoLog(LocalizedStrings.Str2306Params, _settings.StartFrom);

				foreach (var year in Competition.AllYears.Where(d => d.Year >= _settings.StartFrom.Year).ToArray())
				{
					if (!CanProcess())
						break;

					this.AddInfoLog(LocalizedStrings.Str2827Params, year);

					var yearCompetition = competition.Get(year);

					foreach (var date in yearCompetition.Days.Where(d => d >= _settings.StartFrom).ToArray())
					{
						if (!CanProcess())
							break;

						if (_settings.IgnoreWeekends && !ExchangeBoard.Forts.WorkingTime.IsTradeDate(date, true))
						{
							this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
							continue;
						}

						var canUpdateFrom = true;

						foreach (var member in yearCompetition.Members)
						{
							if (!CanProcess())
							{
								canUpdateFrom = false;
								break;
							}

							var trades = yearCompetition.GetTrades(EntityRegistry.Securities, member, date);

							if (trades.Any())
							{
								foreach (var group in trades.GroupBy(i => i.Order.Security))
									SaveOrderLog(group.Key, group.OrderBy(i => i.Order.Time));	
							}
							else
								this.AddDebugLog(LocalizedStrings.NoData);
						}

						if (canUpdateFrom)
						{
							_settings.StartFrom = date;
							SaveSettings();
						}
					}
				}
			}
			else
			{
				this.AddInfoLog(LocalizedStrings.Str2828Params, _settings.StartFrom);
			}

			return base.OnProcess();
		}
	}
}