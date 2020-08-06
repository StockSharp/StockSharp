namespace SampleConnection
{
	using System;

	using Ecng.ComponentModel;

	using StockSharp.Messages;

	public partial class DatesWindow
	{
		private class SettingsObject : NotifiableObject
		{
			private DateTimeOffset? _from;

			public DateTimeOffset? From
			{
				get => _from;
				set
				{
					_from = value;
					NotifyChanged(nameof(From));
				}
			}

			private DateTimeOffset? _to;

			public DateTimeOffset? To
			{
				get => _to;
				set
				{
					_to = value;
					NotifyChanged(nameof(To));
				}
			}

			public MarketDataBuildModes BuildMode { get; set; } = MarketDataBuildModes.LoadAndBuild;
		}

		private readonly SettingsObject _settings = new SettingsObject();

		public DatesWindow()
		{
			InitializeComponent();

			PropGrid.SelectedObject = _settings;
		}

		public DateTimeOffset? From
		{
			get => _settings.From;
			set => _settings.From = value?.Date;
		}

		public DateTimeOffset? To
		{
			get => _settings.To;
			set => _settings.To = value?.Date;
		}

		public MarketDataBuildModes BuildMode
		{
			get => _settings.BuildMode;
			set => _settings.BuildMode = value;
		}
	}
}