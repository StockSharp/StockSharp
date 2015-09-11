namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Messages;

	/// <summary>
	/// The table to display a message <see cref="Level1ChangeMessage"/>.
	/// </summary>
	public partial class Level1Grid
	{
		private readonly ThreadSafeObservableCollection<Level1ChangeMessage> _messages;

		/// <summary>
		/// Initializes a new instance of the <see cref="Level1Grid"/>.
		/// </summary>
		public Level1Grid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<Level1ChangeMessage>();
			ItemsSource = itemsSource;

			_messages = new ThreadSafeObservableCollection<Level1ChangeMessage>(itemsSource) { MaxCount = 10000 };
		}

		/// <summary>
		/// The maximum number of messages to display. The -1 value means an unlimited amount. The default value is 10000.
		/// </summary>
		public int MaxCount
		{
			get { return _messages.MaxCount; }
			set { _messages.MaxCount = value; }
		}

		/// <summary>
		/// The list of messages added to the table.
		/// </summary>
		public IListEx<Level1ChangeMessage> Messages
		{
			get { return _messages; }
		}

		/// <summary>
		/// The selected message.
		/// </summary>
		public Level1ChangeMessage SelectedMessage
		{
			get { return SelectedMessages.FirstOrDefault(); }
		}

		/// <summary>
		/// Selected messages.
		/// </summary>
		public IEnumerable<Level1ChangeMessage> SelectedMessages
		{
			get { return SelectedItems.Cast<Level1ChangeMessage>(); }
		}
	}
}