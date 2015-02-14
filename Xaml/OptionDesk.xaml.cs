namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Windows.Data;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Xaml.Grids;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Derivatives;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Доска опционов.
	/// </summary>
	public partial class OptionDesk
	{
		private bool _updateSource = true;

		/// <summary>
		/// Создать <see cref="OptionDesk"/>.
		/// </summary>
		public OptionDesk()
		{
			InitializeComponent();

			#region Format rules

			var callColor = Color.FromArgb(255, 190, 224, 189);
			var putColor = Color.FromArgb(255, 255, 190, 189);

			FormatRules.Add(CallRho, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallTheta, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallVega, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallGamma, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallDelta, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallBid, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallAsk, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallTheorPrice, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallVolume, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallOpenInterest, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(CallSecCode, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(callColor),
			});
			FormatRules.Add(Strike, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = Brushes.LightGray,
				Font = { Weight = System.Windows.FontWeights.Bold }
			});
			FormatRules.Add(ImpliedVolatility, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = Brushes.LightGray,
			});
			FormatRules.Add(HistoricalVolatility, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = Brushes.LightGray,
			});
			FormatRules.Add(PnL, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = Brushes.LightGray,
			});
			FormatRules.Add(PutSecCode, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutOpenInterest, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutVolume, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutTheorPrice, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutBid, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutAsk, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutDelta, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutGamma, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutVega, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutTheta, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});
			FormatRules.Add(PutRho, new FormatRule
			{
				Condition = ComparisonOperator.Any,
				Background = new SolidColorBrush(putColor),
			});

			#endregion

			Options = Enumerable.Empty<Security>();
		}

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; set; }

		/// <summary>
		/// Поставщик маркет-данных.
		/// </summary>
		public IMarketDataProvider MarketDataProvider { get; set; }

		/// <summary>
		/// Текущее время. Если оно установлено, то <see cref="IBlackScholes"/> использует это время.
		/// </summary>
		public DateTime? CurrentTime { get; set; }

		/// <summary>
		/// Текущая цена базового актива. Если она установлено, то <see cref="IBlackScholes"/> использует эту цену.
		/// </summary>
		public decimal? AssetPrice { get; set; }

		private bool _useBlackModel;

		/// <summary>
		/// Использовать модель <see cref="Black"/> вместо модели <see cref="IBlackScholes"/>.
		/// По-умолчанию выключено.
		/// </summary>
		public bool UseBlackModel
		{
			get { return _useBlackModel; }
			set
			{
				if (_useBlackModel == value)
					return;

				_useBlackModel = value;

				((OptionDeskRow[])ItemsSource).ForEach(r =>
				{
					if (r.Call != null)
						r.Call.ApplyModel();

					if (r.Put != null)
						r.Put.ApplyModel();
				});
			}
		}

		private IEnumerable<Security> _options;

		/// <summary>
		/// Страйк-опционы.
		/// </summary>
		public IEnumerable<Security> Options
		{
			get { return _options; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				var group = value.GroupBy(o => o.GetUnderlyingAsset(SecurityProvider));

				if (group.Count() > 1)
					throw new ArgumentException(LocalizedStrings.Str1524);

				_options = value;

				_updateSource = true;
			}
		}

		/// <summary>
		/// Обновить значения доски.
		/// </summary>
		public void RefreshOptions()
		{
			if (_updateSource)
			{
				ItemsSource = _options.GroupBy(o => o.Strike).Select(g =>
					new OptionDeskRow(this,
						g.FirstOrDefault(o => o.OptionType == OptionTypes.Call),
						g.FirstOrDefault(o => o.OptionType == OptionTypes.Put)))
					.ToArray();

				_updateSource = false;
			}

			var rows = (OptionDeskRow[])ItemsSource;

			if (MarketDataProvider == null || SecurityProvider == null)
				return;

			decimal maxIV = 0;
			decimal maxHV = 0;
			decimal maxPnL = 0;
			decimal maxCallVolume = 0;
			decimal maxPutVolume = 0;
			decimal maxCallOI = 0;
			decimal maxPutOI = 0;

			var now = CurrentTime ?? TimeHelper.Now;

			foreach (var row in rows)
			{
				var call = row.Call;

				if (call != null)
				{
					decimal? iv, hv, oi, volume;
					Update(call, now, out iv, out hv, out oi, out volume);

					maxIV = maxIV.Max(iv ?? 0m);
					maxHV = maxHV.Max(hv ?? 0m);

					maxCallVolume = maxCallVolume.Max(volume ?? 0m);
					maxCallOI = maxCallOI.Max(oi ?? 0m);
				}

				var put = row.Put;

				if (put != null)
				{
					decimal? iv, hv, oi, volume;
					Update(put, now, out iv, out hv, out oi, out volume);

					maxIV = maxIV.Max(iv ?? 0m);
					maxHV = maxHV.Max(hv ?? 0m);

					maxPutVolume = maxPutVolume.Max(volume ?? 0m);
					maxPutOI = maxPutOI.Max(oi ?? 0m);
				}

				maxPnL = maxPnL.Max(row.PnL);
			}

			rows.ForEach(row =>
			{
				row.MaxImpliedVolatility = maxIV;
				row.MaxHistoricalVolatility = maxHV;
				row.MaxPnL = maxPnL;

				if (row.Call != null)
				{
					row.Call.MaxOpenInterest = maxCallOI;
					row.Call.MaxVolume = maxCallVolume;
				}

				if (row.Put != null)
				{
					row.Put.MaxOpenInterest = maxPutOI;
					row.Put.MaxVolume = maxPutVolume;
				}

				row.Notify("ImpliedVolatility");
				row.Notify("MaxHistoricalVolatility");
				row.Notify("HistoricalVolatility");
				row.Notify("MaxHistoricalVolatility");
				row.Notify("MaxPnL");
				row.Notify("Call");
				row.Notify("Put");
			});
		}

		//private void MarketDataProviderOnValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTime serverTime, DateTime localTime)
		//{
		//	var item = _optionDeskRowSides.TryGetValue(security);

		//	if (item == null)
		//		return;

		//	Update(item, changes);
		//}

		private void Update(OptionDeskRow.OptionDeskRowSide side, DateTime now, out decimal? iv, out decimal? hv, out decimal? oi, out decimal? volume)
		{
			iv = hv = oi = volume = null;

			var option = side.Option;

			var changes = MarketDataProvider.GetSecurityValues(option);

			if (changes == null)
				return;

			foreach (var change in changes)
			{
				switch (change.Key)
				{
					case Level1Fields.BestAskPrice:
						side.BestAskPrice = (decimal)change.Value;
						break;
					case Level1Fields.BestBidPrice:
						side.BestBidPrice = (decimal)change.Value;
						break;
					case Level1Fields.Volume:
						volume = side.Volume = (decimal)change.Value;
						break;
					case Level1Fields.OpenInterest:
						oi = side.OpenInterest = (decimal)change.Value;
						break;
					case Level1Fields.ImpliedVolatility:
						iv = (decimal)change.Value;
						break;
					case Level1Fields.HistoricalVolatility:
						hv = (decimal)change.Value;
						break;
				}
			}

			const int round = 2;

			var bs = side.Model;

			side.TheorPrice = decimal.Round(bs.Premium(now, assetPrice: AssetPrice), round);
			side.Delta = decimal.Round(bs.Delta(now, assetPrice: AssetPrice), round);
			side.Gamma = decimal.Round(bs.Gamma(now, assetPrice: AssetPrice), round);
			side.Theta = decimal.Round(bs.Theta(now, assetPrice: AssetPrice), round);
			side.Vega = decimal.Round(bs.Vega(now, assetPrice: AssetPrice), round);
			side.Rho = decimal.Round(bs.Rho(now, assetPrice: AssetPrice), round);

			var assetPrice = bs.GetAssetPrice(AssetPrice);

			if (assetPrice == 0)
				side.PnL = 0;
			else
			{
				side.PnL = (option.OptionType == OptionTypes.Call ? 1 : -1) * (assetPrice - option.Strike);

				if (side.PnL < 0)
					side.PnL = 0;	
			}
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("UseBlackModel", UseBlackModel);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			UseBlackModel = storage.GetValue<bool>("UseBlackModel");
		}
	}

	sealed class NullableDecimalValueConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value ?? 0m;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}