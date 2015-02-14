namespace StockSharp.Algo.Strategies.Analytics
{
	using System;
	using System.ComponentModel;
	using System.Globalization;

	using Abt.Controls.SciChart.Visuals;

	using Ecng.Common;
	using Ecng.Xaml.Grids;

	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Базовая аналитическая стратегия.
	/// </summary>
	[CategoryOrderLoc(LocalizedStrings.Str1221Key, 0)]
	public abstract class BaseAnalyticsStrategy : Strategy
	{
		private readonly StrategyParam<DateTime> _from;

		/// <summary>
		/// Дата начала.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[DescriptionLoc(LocalizedStrings.Str1222Key)]
		[CategoryLoc(LocalizedStrings.Str1221Key)]
		[PropertyOrder(0)]
		public DateTime From
		{
			get { return _from.Value; }
			set { _from.Value = value; }
		}

		private readonly StrategyParam<DateTime> _to;

		/// <summary>
		/// Дата окончания.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[DescriptionLoc(LocalizedStrings.Str418Key, true)]
		[CategoryLoc(LocalizedStrings.Str1221Key)]
		[PropertyOrder(1)]
		public DateTime To
		{
			get { return _to.Value; }
			set { _to.Value = value; }
		}

		/// <summary>
		/// Хранилище маркет-данных.
		/// </summary>
		[Browsable(false)]
		public IStorageRegistry StorateRegistry { get; private set; }

		/// <summary>
		/// Инициализировать <see cref="BaseAnalyticsStrategy"/>.
		/// </summary>
		protected BaseAnalyticsStrategy()
		{
			_from = this.Param<DateTime>("From");
			_to = this.Param("To", DateTime.MaxValue);
		}

		/// <summary>
		/// Отменить все активные заявки (стоп и обычные).
		/// </summary>
		protected override void ProcessCancelActiveOrders()
		{
		}

		/// <summary>
		/// Текущее время, которое будет передано в <see cref="LogMessage.Time"/>.
		/// </summary>
		public override DateTimeOffset CurrentTime
		{
			get { return TimeHelper.Now; }
		}

		/// <summary>
		/// График.
		/// </summary>
		[CLSCompliant(false)]
		protected SciChartSurface Chart
		{
			get { return Environment.GetValue<SciChartSurface>("Chart"); }
		}

		/// <summary>
		/// Таблица.
		/// </summary>
		protected UniversalGrid Grid
		{
			get { return Environment.GetValue<UniversalGrid>("Grid"); }
		}

		/// <summary>
		/// Формат данных.
		/// </summary>
		protected StorageFormats StorageFormat
		{
			get { return Environment.GetValue<StorageFormats>("StorageFormat"); }
		}

		/// <summary>
		/// Метод вызывается тогда, когда вызвался метод <see cref="Strategy.Start"/>, и состояние <see cref="Strategy.ProcessState"/> перешло в значение <see cref="ProcessStates.Started"/>.
		/// </summary>
		protected override void OnStarted()
		{
			var storateRegistry = new StorageRegistry();

			var drive = Environment.GetValue<IMarketDataDrive>("Drive");
			if (drive != null)
				storateRegistry.DefaultDrive = drive;

			StorateRegistry = storateRegistry;

			ThreadingHelper
				.Thread(() =>
				{
					try
					{
						OnAnalyze();
					}
					catch (Exception ex)
					{
						this.AddErrorLog(ex);
						ex.LogError();
					}
				})
				.Name("{0} analyze thread.".Put(Name))
				.Culture(CultureInfo.InvariantCulture)
				.Launch();
		}

		/// <summary>
		/// Анализировать.
		/// </summary>
		protected abstract void OnAnalyze();
	}
}