namespace StockSharp.Algo.Strategies.Analytics
{
	using System;
	using System.ComponentModel;
	using System.Globalization;

	using Ecng.Xaml.Charting.Visuals;
	using Ecng.Common;
	using Ecng.Xaml.Grids;

	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The base analytic strategy.
	/// </summary>
	[CategoryOrderLoc(LocalizedStrings.Str1221Key, 0)]
	public abstract class BaseAnalyticsStrategy : Strategy
	{
		private readonly StrategyParam<DateTime> _from;

		/// <summary>
		/// Start date.
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
		/// End date.
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
		/// Market-data storage.
		/// </summary>
		[Browsable(false)]
		public IStorageRegistry StorateRegistry { get; private set; }

		/// <summary>
		/// Initialize <see cref="BaseAnalyticsStrategy"/>.
		/// </summary>
		protected BaseAnalyticsStrategy()
		{
			_from = this.Param<DateTime>("From");
			_to = this.Param("To", DateTime.MaxValue);
		}

		/// <summary>
		/// To cancel all active orders (to stop and regular).
		/// </summary>
		protected override void ProcessCancelActiveOrders()
		{
		}

		/// <summary>
		/// Current time, which will be passed to the <see cref="LogMessage.Time"/>.
		/// </summary>
		public override DateTimeOffset CurrentTime => TimeHelper.NowWithOffset;

		/// <summary>
		/// Chart.
		/// </summary>
		[CLSCompliant(false)]
		protected UltrachartSurface Chart => Environment.GetValue<UltrachartSurface>("Chart");

		/// <summary>
		/// Table.
		/// </summary>
		protected UniversalGrid Grid => Environment.GetValue<UniversalGrid>("Grid");

		/// <summary>
		/// Data format.
		/// </summary>
		protected StorageFormats StorageFormat => Environment.GetValue<StorageFormats>("StorageFormat");

		/// <summary>
		/// The method is called when the <see cref="Strategy.Start"/> method has been called and the <see cref="Strategy.ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
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
		/// To analyze.
		/// </summary>
		protected abstract void OnAnalyze();
	}
}