namespace OptionCalculator
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Windows;
	using System.Windows.Controls;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Quik;
	using StockSharp.Algo.Derivatives;
	using StockSharp.Plaza;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private readonly ThreadSafeObservableCollection<Security> _assets;
		private IConnector _connector;

		private class FakeConnector : Connector, IMarketDataProvider
		{
			private readonly IEnumerable<Security> _securities;

			public FakeConnector(IEnumerable<Security> securities)
			{
				_securities = securities;
			}

			public override IEnumerable<Security> Securities
			{
				get { return _securities; }
			}

			public override DateTimeOffset CurrentTime
			{
				get { return DateTime.Now; }
			}

			object IMarketDataProvider.GetSecurityValue(Security security, Level1Fields field)
			{
				switch (field)
				{
					case Level1Fields.OpenInterest:
						return security.OpenInterest;

					case Level1Fields.ImpliedVolatility:
						return security.ImpliedVolatility;

					case Level1Fields.HistoricalVolatility:
						return security.HistoricalVolatility;

					case Level1Fields.Volume:
						return security.Volume;

					case Level1Fields.LastTradePrice:
						return security.LastTrade == null ? (decimal?)null : security.LastTrade.Price;

					case Level1Fields.LastTradeVolume:
						return security.LastTrade == null ? (decimal?)null : security.LastTrade.Volume;

					case Level1Fields.BestBidPrice:
						return security.BestBid == null ? (decimal?)null : security.BestBid.Price;

					case Level1Fields.BestBidVolume:
						return security.BestBid == null ? (decimal?)null : security.BestBid.Volume;

					case Level1Fields.BestAskPrice:
						return security.BestAsk == null ? (decimal?)null : security.BestAsk.Price;

					case Level1Fields.BestAskVolume:
						return security.BestAsk == null ? (decimal?)null : security.BestAsk.Volume;
				}

				return null;
			}

			IEnumerable<Level1Fields> IMarketDataProvider.GetLevel1Fields(Security security)
			{
				return new []
				{
					Level1Fields.OpenInterest,
					Level1Fields.ImpliedVolatility,
					Level1Fields.HistoricalVolatility,
					Level1Fields.Volume,
					Level1Fields.LastTradePrice,
					Level1Fields.LastTradeVolume,
					Level1Fields.BestBidPrice,
					Level1Fields.BestAskPrice,
					Level1Fields.BestBidVolume,
					Level1Fields.BestAskVolume
				};
			}
		}

		private readonly HashSet<Security> _options = new HashSet<Security>();

		public MainWindow()
		{
			InitializeComponent();

			var assetsSource = new ObservableCollectionEx<Security>();
			Assets.ItemsSource = assetsSource;
			_assets = new ThreadSafeObservableCollection<Security>(assetsSource);

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			Path.Text = QuikTerminal.GetDefaultPath();

			//
			// добавляем тестовый данные для отображения доски опционов

			var asset = new Security { Id = "RIU4@FORTS", LastTrade = new Trade { Price = 56000 } };

			var connector = new FakeConnector(new[] { asset });

			var expiryDate = new DateTime(2014, 09, 15);

			Desk.MarketDataProvider = connector;
			Desk.SecurityProvider = connector;
			Desk.CurrentTime = new DateTime(2014, 08, 15);

			Desk.Options = new[]
			{
				CreateStrike(05000, 10, 122, OptionTypes.Call, expiryDate, asset, 100),
				CreateStrike(10000, 10, 110, OptionTypes.Call, expiryDate, asset, 343),
				CreateStrike(15000, 10, 100, OptionTypes.Call, expiryDate, asset, 3454),
				CreateStrike(20000, 78, 85, OptionTypes.Call, expiryDate, asset, null),
				CreateStrike(25000, 32, 65, OptionTypes.Call, expiryDate, asset, 100),
				CreateStrike(30000, 3245, 30, OptionTypes.Call, expiryDate, asset, 55),
				CreateStrike(35000, 3454, 65, OptionTypes.Call, expiryDate, asset, 456),
				CreateStrike(40000, 34, 85, OptionTypes.Call, expiryDate, asset, 4),
				CreateStrike(45000, 3566, 100, OptionTypes.Call, expiryDate, asset, 67),
				CreateStrike(50000, 454, 110, OptionTypes.Call, expiryDate, asset, null),
				CreateStrike(55000, 10, 122, OptionTypes.Call, expiryDate, asset, 334),

				CreateStrike(05000, 10, 122, OptionTypes.Put, expiryDate, asset, 100),
				CreateStrike(10000, 10, 110, OptionTypes.Put, expiryDate, asset, 343),
				CreateStrike(15000, 6788, 100, OptionTypes.Put, expiryDate, asset, 3454),
				CreateStrike(20000, 10, 85, OptionTypes.Put, expiryDate, asset, null),
				CreateStrike(25000, 567, 65, OptionTypes.Put, expiryDate, asset, 100),
				CreateStrike(30000, 4577, 30, OptionTypes.Put, expiryDate, asset, 55),
				CreateStrike(35000, 67835, 65, OptionTypes.Put, expiryDate, asset, 456),
				CreateStrike(40000, 13245, 85, OptionTypes.Put, expiryDate, asset, 4),
				CreateStrike(45000, 10, 100, OptionTypes.Put, expiryDate, asset, 67),
				CreateStrike(50000, 454, 110, OptionTypes.Put, expiryDate, asset, null),
				CreateStrike(55000, 10, 122, OptionTypes.Put, expiryDate, asset, 334)
			};

			Desk.RefreshOptions();
		}

		private static Security CreateStrike(decimal strike, decimal oi, decimal iv, OptionTypes type, DateTime expiryDate, Security asset, decimal? lastTrade)
		{
			var s = new Security
			{
				Code = "RI {0} {1}".Put(type == OptionTypes.Call ? 'C' : 'P', strike),
				Strike = strike,
				OpenInterest = oi,
				ImpliedVolatility = iv,
				HistoricalVolatility = iv,
				OptionType = type,
				ExpiryDate = expiryDate,
				Board = ExchangeBoard.Forts,
				UnderlyingSecurityId = asset.Id,
				LastTrade = lastTrade == null ? null : new Trade { Price = lastTrade.Value },
				Volume = RandomGen.GetInt(10000)
			};

			s.BestBid = new Quote(s, s.StepPrice ?? 1m * RandomGen.GetInt(100), s.VolumeStep ?? 1m * RandomGen.GetInt(100), Sides.Buy);
			s.BestAsk = new Quote(s, s.BestBid.Price.Max(s.StepPrice ?? 1m * RandomGen.GetInt(100)), s.VolumeStep ?? 1m * RandomGen.GetInt(100), Sides.Sell);

			return s;
		}

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!Path.Text.IsEmpty())
				dlg.SelectedPath = Path.Text;

			if (dlg.ShowDialog(this) == true)
			{
				Path.Text = dlg.SelectedPath;
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_connector != null)
			{
				_connector.Dispose();
			}

			base.OnClosing(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (_connector == null)
			{
				if (IsQuik.IsChecked == true)
				{
					var isDde = IsDde.IsChecked == true;

					if (isDde && Path.Text.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2969);
						return;
					}

					// создаем подключение
					var trader = new QuikTrader(Path.Text)
					{
						IsDde = isDde
					};

					if (isDde)
					{
						// изменяем метаданные так, чтобы начали обрабатывать дополнительные колонки опционов
						var columns = trader.SecuritiesTable.Columns;
						columns.Add(DdeSecurityColumns.Strike);
						columns.Add(DdeSecurityColumns.ImpliedVolatility);
						columns.Add(DdeSecurityColumns.UnderlyingSecurity);
						columns.Add(DdeSecurityColumns.TheorPrice);
						columns.Add(DdeSecurityColumns.OptionType);
						columns.Add(DdeSecurityColumns.ExpiryDate);

						trader.DdeTables = new[] { trader.SecuritiesTable, trader.TradesTable };
					}

					_connector = trader;
				}
				else
				{
					var trader = new PlazaTrader
					{
						Address = Address.Text.To<EndPoint>(),
						IsCGate = IsCGate.IsChecked == true
					};

					trader.Tables.Add(trader.TableRegistry.Volatility);

					if (IsAutorization.IsChecked == true)
					{
						trader.Login = Login.Text;
						trader.Password = Password.Password;
					}

					_connector = trader;
				}

				Desk.MarketDataProvider = _connector;
				Desk.SecurityProvider = _connector;
				Desk.CurrentTime = null;

				// добавляем в выпадающий список только фьючерсы
				_connector.NewSecurities += securities =>
					this.GuiAsync(() =>
					{
						_assets.AddRange(securities.Where(s => s.Type == SecurityTypes.Future));

						if (SelectedAsset == null && _assets.Count > 0)
							SelectedAsset = _assets.First();

						if (SelectedAsset != null)
						{
							var newStrikes = securities
								.Where(s => s.Type == SecurityTypes.Option && s.UnderlyingSecurityId.CompareIgnoreCase(SelectedAsset.Id))
								.ToArray();

							if (newStrikes.Length > 0)
							{
								_options.AddRange(newStrikes);
								Desk.Options = _options;
								Desk.RefreshOptions();
							}
						}
					});

				_connector.SecuritiesChanged += securities =>
				{
					this.GuiAsync(() =>
					{
						if (SelectedAsset == null)
							return;

						var newStrikes = securities
								.Where(s => s.Type == SecurityTypes.Option && s.UnderlyingSecurityId.CompareIgnoreCase(SelectedAsset.Id))
								.Where(s => !_options.Contains(s))
								.ToArray();

						if (newStrikes.Length > 0)
						{
							_options.AddRange(newStrikes);
							Desk.Options = _options;
							Desk.RefreshOptions();
						}

						if (Desk.Options.Intersect(securities).Any())
							Desk.RefreshOptions();
					});
				};

				// подписываемся на событие новых сделок чтобы обновить текущую цену фьючерса
				_connector.NewTrades += trades => this.GuiAsync(() =>
				{
					var asset = SelectedAsset;
					if (asset == null)
						return;

					if (asset.LastTrade != null)
						LastPrice.Text = asset.LastTrade.Price.To<string>();
				});
			}

			if (_connector.ConnectionState == ConnectionStates.Connected)
				_connector.Disconnect();
			else
				_connector.Connect();
		}

		private Security SelectedAsset
		{
			get { return (Security)Assets.SelectedItem; }
			set { Assets.SelectedItem = value; }
		}

		private void Assets_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!Desk.Options.IsEmpty())
				Desk.Options.ForEach(s => _connector.UnRegisterSecurity(s));

			var asset = SelectedAsset;

			LastPrice.Text = asset != null && asset.LastTrade != null
				? asset.LastTrade.Price.To<string>()
				: string.Empty;

			var derivatives = asset.GetDerivatives(_connector, ExpiryDate.Value).ToArray();

			_options.Clear();
			_options.AddRange(derivatives);

			derivatives.ForEach(s => _connector.RegisterSecurity(s));

			Desk.Options = derivatives;
			Desk.RefreshOptions();
		}
	}
}