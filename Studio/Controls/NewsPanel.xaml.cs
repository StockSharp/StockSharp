#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: NewsPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System.Windows;

	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.NewsKey)]
	[DescriptionLoc(LocalizedStrings.Str3238Key)]
	[Icon("images/news_24x24.png")]
	public partial class NewsPanel
	{
		#region Dependency properties

		public static readonly DependencyProperty SubscribeNewsProperty = DependencyProperty.Register(nameof(SubscribeNews), typeof(bool), typeof(NewsPanel), new PropertyMetadata(SubscribeNewsChanged));

		private static void SubscribeNewsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (NewsPanel)sender;

			if ((bool)args.NewValue) 
				ConfigManager.GetService<IConnector>().RegisterNews();
			else
				ConfigManager.GetService<IConnector>().UnRegisterNews();

			new ControlChangedCommand(ctrl).Process(ctrl);
		}

		public bool SubscribeNews
		{
			get { return (bool)GetValue(SubscribeNewsProperty); }
			set { SetValue(SubscribeNewsProperty, value); }
		}

		#endregion

		public NewsPanel()
		{
			InitializeComponent();

			NewsGrid.PropertyChanged += (s, e) => RaiseChangedCommand();
			NewsGrid.SelectionChanged += (s, e) => new SelectCommand<News>(NewsGrid.SelectedNews, false).Process(this);

			AlertBtn.SchemaChanged += RaiseChangedCommand;

			GotFocus += (s, e) => new SelectCommand<News>(NewsGrid.SelectedNews, false).Process(this);

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<NewNewsCommand>(this, false, cmd =>
			{
				NewsGrid.News.Add(cmd.News);

				AlertBtn.Process(cmd.News.ToMessage());
			});
			cmdSvc.Register<ResetedCommand>(this, false, cmd => NewsGrid.News.Clear());
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("SubscribeNews", SubscribeNews);
			storage.SetValue("NewsGrid", NewsGrid.Save());
			storage.SetValue("AlertSettings", AlertBtn.Save());
		}

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			SubscribeNews = storage.GetValue("SubscribeNews", false);

			NewsGrid.NewsProvider = ConfigManager.GetService<INewsProvider>();
			NewsGrid.Load(storage.GetValue<SettingsStorage>("NewsGrid"));

			var alertSettings = storage.GetValue<SettingsStorage>("AlertSettings");
			if (alertSettings != null)
				AlertBtn.Load(alertSettings);
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<NewNewsCommand>(this);
			cmdSvc.UnRegister<ResetedCommand>(this);

			AlertBtn.Dispose();
		}
	}
}