namespace StockSharp.Studio.Controls
{
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	/// <summary>
	/// Визуальный контрол-таблица, отображающая сделки (коллекцию объектов класса <see cref="MyTrade"/>).
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str985Key)]
	[DescriptionLoc(LocalizedStrings.Str3271Key)]
	[Icon("images/deal_24x24.png")]
	public partial class MyTradesTable
	{
		/// <summary>
		/// Создать <see cref="MyTradesTable"/>.
		/// </summary>
		public MyTradesTable()
		{
			InitializeComponent();

			TradesGrid.PropertyChanged += (s, e) => new ControlChangedCommand(this).Process(this);
			TradesGrid.SelectionChanged += (s, e) => new SelectCommand<MyTrade>(TradesGrid.SelectedTrade, false).Process(this);

			GotFocus += (s, e) => new SelectCommand<MyTrade>(TradesGrid.SelectedTrade, false).Process(this);

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<NewMyTradesCommand>(this, false, cmd => TradesGrid.Trades.AddRange(cmd.Trades));
			cmdSvc.Register<ResetedCommand>(this, false, cmd => TradesGrid.Trades.Clear());
		}

		public override void Save(SettingsStorage settings)
		{
			settings.SetValue("TradesGrid", TradesGrid.Save());
		}

		public override void Load(SettingsStorage settings)
		{
			TradesGrid.Load(settings.GetValue<SettingsStorage>("TradesGrid"));
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<NewMyTradesCommand>(this);
			cmdSvc.UnRegister<ResetedCommand>(this);
		}
	}
}