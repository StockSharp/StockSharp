#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: MyTradesTable.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			base.Save(settings);

			settings.SetValue("TradesGrid", TradesGrid.Save());
		}

		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

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