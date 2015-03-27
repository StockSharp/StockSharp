namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	public partial class NewsPane
	{
		public NewsPane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetNews);
		}

		protected override Type DataType
		{
			get { return typeof(NewsMessage); }
		}

		public override string Title
		{
			get { return LocalizedStrings.News; }
		}

		public override Security SelectedSecurity
		{
			get { return null; }
			set { }
		}

		private void OnDateValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Progress.ClearStatus();
		}

		private IEnumerableEx<NewsMessage> GetNews()
		{
			// TODO
			var news = ConfigManager.GetService<IEntityRegistry>().News;
			return news
				.Where(n => n.ServerTime >= From && n.ServerTime <= To + TimeHelper.LessOneDay)
				.Select(n => n.ToMessage())
				.ToEx(news.Count);
		}

		private void Find_OnClick(object sender, RoutedEventArgs e)
		{
			NewsPanel.NewsGrid.Messages.Clear();
			Progress.Load(GetNews(), NewsPanel.NewsGrid.Messages.AddRange, 1000);
		}
	}
}