namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	public partial class NewsPane : IPane
	{
		private readonly IEntityRegistry _entityRegistry;

		public NewsPane()
		{
			InitializeComponent();

			Progress.Init(ExportBtn, MainGrid);

			From = DateTime.Today - TimeSpan.FromDays(7);
			To = DateTime.Today + TimeSpan.FromDays(1);

			_entityRegistry = ConfigManager.GetService<IEntityRegistry>();

			ExportBtn.EnableType(ExportTypes.Bin, false);
		}

		string IPane.Title
		{
			get { return LocalizedStrings.News; }
		}

		Uri IPane.Icon
		{
			get { return null; }
		}

		bool IPane.IsValid
		{
			get { return true; }
		}

		private DateTime? From
		{
			get { return FromCtrl.Value; }
			set { FromCtrl.Value = value; }
		}

		private DateTime? To
		{
			get { return ToCtrl.Value; }
			set { ToCtrl.Value = value; }
		}

		private void OnDateValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Progress.ClearStatus();
		}

		private IEnumerableEx<News> GetNews()
		{
			return _entityRegistry.News.ToEx();
		}

		private void ExportBtn_OnExportStarted()
		{
			var news = GetNews();

			if (news.Count == 0)
			{
				Progress.DoesntExist();
				return;
			}

			var path = ExportBtn.GetPath(null, typeof(News), null, From, To, null);

			if (path == null)
				return;

			Progress.Start(null, typeof(News), null, news, path);
		}

		public void Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("From"))
				From = storage.GetValue<DateTime>("From");

			if (storage.ContainsKey("To"))
				To = storage.GetValue<DateTime>("To");
		}

		public void Save(SettingsStorage storage)
		{
			if (From != null)
				storage.SetValue("From", (DateTime)From);

			if (To != null)
				storage.SetValue("To", (DateTime)To);
		}

		void IDisposable.Dispose()
		{
			Progress.Stop();
		}

		private void Find_OnClick(object sender, RoutedEventArgs e)
		{
			NewsPanel.NewsGrid.News.Clear();
			Progress.Load(GetNews(), NewsPanel.NewsGrid.News.AddRange, 1000);
		}
	}
}