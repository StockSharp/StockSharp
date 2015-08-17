namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

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
			return ConfigManager
				.GetService<IStorageRegistry>()
				.GetNewsMessageStorage(Drive, StorageFormat)
				.Load(From, To + TimeHelper.LessOneDay);
		}

		private void Find_OnClick(object sender, RoutedEventArgs e)
		{
			NewsPanel.NewsGrid.Messages.Clear();
			Progress.Load(GetNews(), NewsPanel.NewsGrid.Messages.AddRange, 1000);
		}

		protected override bool CheckSecurity()
		{
			return true;
		}
	}
}