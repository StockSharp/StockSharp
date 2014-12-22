namespace StockSharp.Hydra.Panes
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Localization;

	public partial class ExchangeBoardPane : IPane
	{
		public ExchangeBoardPane()
		{
			InitializeComponent();
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Editor.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			Editor.Save(storage);
		}

		void IDisposable.Dispose()
		{
		}

		string IPane.Title
		{
			get { return LocalizedStrings.Str2831; }
		}

		Uri IPane.Icon
		{
			get { return null; }
		}

		bool IPane.IsValid
		{
			get { return true; }
		}
	}
}