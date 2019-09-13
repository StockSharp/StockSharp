namespace SampleConnectionWithStorage
{
	using System;

	using Ecng.ComponentModel;

	public partial class DatesWindow
	{
		private class DatesRange : NotifiableObject
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
		}

		private readonly DatesRange _range = new DatesRange();

		public DatesWindow()
		{
			InitializeComponent();

			PropGrid.SelectedObject = _range;
		}

		public DateTimeOffset? From
		{
			get => _range.From;
			set => _range.From = value;
		}

		public DateTimeOffset? To
		{
			get => _range.To;
			set => _range.To = value;
		}
	}
}