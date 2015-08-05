namespace StockSharp.Hydra.Windows
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Localization;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Messages;

	public partial class ExportTxtPreviewWindow
	{
		public ExportTxtPreviewWindow()
		{
			InitializeComponent();
		}

		public Type DataType { get; set; }
		public object Arg { get; set; }

		public string TxtTemplate
		{
			get { return TxtTemplateCtrl.Text; }
			set { TxtTemplateCtrl.Text = value; }
		}

		private void TxtTemplateCtrl_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			PreviewBtn.IsEnabled = OkBtn.IsEnabled = !TxtTemplate.IsEmpty();
		}

		private void PreviewBtn_OnClick(object sender, RoutedEventArgs e)
		{
			if (DataType == null)
				throw new InvalidOperationException();

			object[] testValues;

			var securityId = new SecurityId
			{
				SecurityCode = "AAPL",
				BoardCode = "NASDAQ"
			};

			var serverTime = new DateTime(1977, 5, 24).ApplyTimeZone(LocalizedStrings.ActiveLanguage == Languages.Russian ? TimeHelper.Moscow : TimeHelper.Est);
			
			if (DataType == typeof(SecurityMessage))
			{
				testValues = new[]
				{
					(object)new SecurityMessage
					{
						SecurityId = securityId,
						Name = "APPLE Inc.",
						Currency = CurrencyTypes.USD,
						Decimals = 2,
						SecurityType = SecurityTypes.Stock
					},
					new SecurityMessage
					{
						SecurityId = new SecurityId
						{
							SecurityCode = "ESU5",
							BoardCode = "NYSE"
						},
						Name = "E-Mini S&P 500 Future",
						Currency = CurrencyTypes.USD,
						Decimals = 2,
						SecurityType = SecurityTypes.Future
					},
					new SecurityMessage
					{
						SecurityId = new SecurityId
						{
							SecurityCode = "ESU5 211500 CA",
							BoardCode = "NYSE"
						},
						Name = "E-mini S&P 500 Option 211500 CA",
						Currency = CurrencyTypes.USD,
						Decimals = 2,
						SecurityType = SecurityTypes.Option,
						Strike = 211500.0m,
						OptionType = OptionTypes.Call
					},
				};
			}
			else if (DataType == typeof(QuoteChangeMessage))
			{
				testValues = new[]
				{
					(object)new TimeQuoteChange
					{
						SecurityId = securityId,
						ServerTime = serverTime,
						Side = Sides.Sell,
						Price = 101.1m,
						Volume = 56
					},
					new TimeQuoteChange
					{
						SecurityId = securityId,
						ServerTime = serverTime,
						Side = Sides.Buy,
						Price = 100.87m,
						Volume = 23
					},
					new TimeQuoteChange
					{
						SecurityId = securityId,
						ServerTime = serverTime,
						Side = Sides.Buy,
						Price = 100.56m,
						Volume = 7
					},

					new TimeQuoteChange
					{
						SecurityId = securityId,
						ServerTime = serverTime,
						Side = Sides.Buy,
						Price = 100.1m,
						Volume = 1
					},
					new TimeQuoteChange
					{
						SecurityId = securityId,
						ServerTime = serverTime,
						Side = Sides.Buy,
						Price = 100.0m,
						Volume = 4
					},
					new TimeQuoteChange
					{
						SecurityId = securityId,
						ServerTime = serverTime,
						Side = Sides.Buy,
						Price = 99.97m,
						Volume = 12
					},
				};
			}
			else if (DataType == typeof(Level1ChangeMessage))
			{
				testValues = new[]
				{
					(object)new Level1ChangeMessage
					{
						SecurityId = securityId,
						ServerTime = serverTime,
					}
					.Add(Level1Fields.OpenPrice, 100.4m)
					.Add(Level1Fields.HighPrice, 120m)
					.Add(Level1Fields.LowPrice, 97.5m)
					.Add(Level1Fields.ClosePrice, 96.0m),
					new Level1ChangeMessage
					{
						SecurityId = securityId,
						ServerTime = serverTime,
					}
					.Add(Level1Fields.LastTradePrice, 100.4m)
					.Add(Level1Fields.LastTradeVolume, 12m),
					new Level1ChangeMessage
					{
						SecurityId = securityId,
						ServerTime = serverTime,
					}
					.Add(Level1Fields.BestBidPrice, 97.4m)
					.Add(Level1Fields.BestBidVolume, 3m)
					.Add(Level1Fields.BestAskPrice, 97.5m)
					.Add(Level1Fields.BestAskVolume, 6m),
				};
			}
			else if (DataType.IsSubclassOf(typeof(CandleMessage)))
			{
				var tf = TimeSpan.FromMinutes(5);

				testValues = new[]
				{
					(object)new TimeFrameCandleMessage
					{
						SecurityId = securityId,
						OpenTime = serverTime,
						TimeFrame = tf,
						OpenPrice = 100.4m,
						HighPrice = 120m,
						LowPrice = 97.5m,
						ClosePrice = 96.0m,
						TotalVolume = 76543
					},
					new TimeFrameCandleMessage
					{
						SecurityId = securityId,
						OpenTime = serverTime + tf,
						TimeFrame = tf,
						OpenPrice = 104.4m,
						HighPrice = 110m,
						LowPrice = 103.5m,
						ClosePrice = 104.0m,
						TotalVolume = 67654
					},
					new TimeFrameCandleMessage
					{
						SecurityId = securityId,
						OpenTime = serverTime + tf + tf,
						TimeFrame = tf,
						OpenPrice = 104.7m,
						HighPrice = 111m,
						LowPrice = 104.5m,
						ClosePrice = 105.0m,
						TotalVolume = 3453
					},
				};
			}
			else if (DataType == typeof(NewsMessage))
			{
				testValues = new[]
				{
					(object)new NewsMessage
					{
						ServerTime = serverTime,
						Headline = "Test",
						BoardCode = "NASDAQ",
						Story = "Test"
					},
					new NewsMessage
					{
						ServerTime = serverTime,
						SecurityId = securityId,
						Headline = "Test",
						Story = "Test"
					},
				};
			}
			else if (DataType == typeof(ExecutionMessage))
			{
				switch ((ExecutionTypes)Arg)
				{
					case ExecutionTypes.Tick:
						testValues = new[]
						{
							(object)new ExecutionMessage
							{
								SecurityId = securityId,
								ServerTime = serverTime,
								ExecutionType = (ExecutionTypes)Arg,
								TradeId = 21354656,
								TradePrice = 103.6m,
								Volume = 45,
							},
							new ExecutionMessage
							{
								SecurityId = securityId,
								ServerTime = serverTime,
								ExecutionType = (ExecutionTypes)Arg,
								TradeId = 21354789,
								TradePrice = 103.7m,
								Volume = 3,
							},
						};
						break;
					case ExecutionTypes.Order:
						testValues = new[]
						{
							(object)new ExecutionMessage
							{
								SecurityId = securityId,
								ServerTime = serverTime,
								ExecutionType = ExecutionTypes.Order,
								PortfolioName = "Account 45-g",
								OrderId = 5421354656,
								Price = 103.6m,
								Side = Sides.Buy,
								TradePrice = 103.6m,
								Volume = 45,
							},
							new ExecutionMessage
							{
								SecurityId = securityId,
								ServerTime = serverTime,
								ExecutionType = ExecutionTypes.Trade,
								PortfolioName = "Account 45-g",
								OrderId = 5421354789,
								Side = Sides.Sell,
								TradeId = 21354789,
								TradePrice = 103.7m,
								Volume = 3,
							},
						};
						break;
					case ExecutionTypes.OrderLog:
						testValues = new[]
						{
							(object)new ExecutionMessage
							{
								SecurityId = securityId,
								ServerTime = serverTime,
								ExecutionType = (ExecutionTypes)Arg,
								OrderId = 5421354656,
								Price = 103.6m,
								Side = Sides.Buy,
								TradePrice = 103.6m,
								Volume = 45,
							},
							new ExecutionMessage
							{
								SecurityId = securityId,
								ServerTime = serverTime,
								ExecutionType = (ExecutionTypes)Arg,
								OrderId = 5421354789,
								Side = Sides.Sell,
								TradeId = 21354789,
								TradePrice = 103.7m,
								Volume = 3,
							},
						};
						break;
					default:
						throw new InvalidOperationException(LocalizedStrings.Str1122Params.Put(Arg));
				}
			}
			else
				throw new InvalidOperationException(LocalizedStrings.Str2142Params.Put(DataType));

			PreviewResult.Text = testValues.Select(v => TxtTemplate.PutEx(v)).Join(Environment.NewLine);
		}

		private void ResetTemplate_OnClick(object sender, RoutedEventArgs e)
		{
			TxtTemplate = DataType.GetTxtTemplate(Arg);
		}
	}
}