namespace StockSharp.Studio.Services
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Logging;
	using StockSharp.Studio.Core;
	using StockSharp.Xaml.Charting;

	class AlgoService : Disposable, IAlgoService
	{
		public AlgoService()
		{
			Connector = new StudioConnector();

			//виртуальные портфели не привязны ни к одному коннектору,
			//поэтому нет необходимости их регистрировать, регистрация
			//в эмуляторе будет выполнена непосредственно перед тестированием.
			//Trader.NewPortfolios += pfs => pfs.ForEach(pf => Trader.RegisterPortfolio(pf));

			ConfigManager.GetService<LogManager>().Sources.Add(Connector);
		}

		IEnumerable<IndicatorType> IAlgoService.IndicatorTypes
		{
			get { return AppConfig.Instance.Indicators; }
		}

		IEnumerable<Type> IAlgoService.CandleTypes
		{
			get { return AppConfig.Instance.Candles; }
		}

		IEnumerable<Type> IAlgoService.DiagramElementTypes
		{
			get { return AppConfig.Instance.DiagramElements; }
		}

		public StudioConnector Connector { get; private set; }

		protected override void DisposeManaged()
		{
			Connector.Dispose();
			base.DisposeManaged();
		}
	}
}