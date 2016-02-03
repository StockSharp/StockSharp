#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: OptionDesk.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// Options board.
	/// </summary>
	public partial class OptionDesk
	{
		private bool _updateSource = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionDesk"/>.
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
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; set; }

		/// <summary>
		/// The market data provider.
		/// </summary>
		public IMarketDataProvider MarketDataProvider { get; set; }

		/// <summary>
		/// Current time. If it is set, then <see cref="IBlackScholes"/> uses this time.
		/// </summary>
		public DateTimeOffset? CurrentTime { get; set; }

		/// <summary>
		/// The current price of the underlying asset. If it is set, then <see cref="IBlackScholes"/> uses this price.
		/// </summary>
		public decimal? AssetPrice { get; set; }

		private bool _useBlackModel;

		/// <summary>
		/// To use the model <see cref="Black"/> instead of <see cref="IBlackScholes"/> model. The default is off.
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
					r.Call?.ApplyModel();
					r.Put?.ApplyModel();
				});
			}
		}

		private IEnumerable<Security> _options;

		/// <summary>
		/// Strike options.
		/// </summary>
		public IEnumerable<Security> Options
		{
			get { return _options; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				var group = value.GroupBy(o => o.GetUnderlyingAsset(SecurityProvider));

				if (group.Count() > 1)
					throw new ArgumentException(LocalizedStrings.Str1524);

				_options = value;

				_updateSource = true;
			}
		}

		/// <summary>
		/// To update the board values.
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

			var now = CurrentTime ?? TimeHelper.NowWithOffset;

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

				maxPnL = maxPnL.Max(row.PnL ?? 0);
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

		private void Update(OptionDeskRow.OptionDeskRowSide side, DateTimeOffset now, out decimal? iv, out decimal? hv, out decimal? oi, out decimal? volume)
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

			var bs = side.Model;

			side.TheorPrice = Round(now, bs.Premium);
			side.Delta = Round(now, bs.Delta);
			side.Gamma = Round(now, bs.Gamma);
			side.Theta = Round(now, bs.Theta);
			side.Vega = Round(now, bs.Vega);
			side.Rho = Round(now, bs.Rho);

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

		private decimal? Round(DateTimeOffset now, Func<DateTimeOffset, decimal?, decimal?, decimal?> func)
		{
			const int round = 2;

			var value = func(now, null, AssetPrice);
			return value == null ? (decimal?)null : decimal.Round(value.Value, round);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(UseBlackModel), UseBlackModel);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			UseBlackModel = storage.GetValue<bool>(nameof(UseBlackModel));
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