namespace StockSharp.Xaml
{
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// The panel for modifing <see cref="IMessageAdapter.SupportedMessages"/>.
	/// </summary>
	public partial class ConnectorSupportedMessagesPanel
	{
		private class SupportedMessage
		{
			private readonly ConnectorSupportedMessagesPanel _parent;
			private readonly IMessageAdapter _adapter;

			public SupportedMessage(ConnectorSupportedMessagesPanel parent, IMessageAdapter adapter, MessageTypes type)
			{
				_parent = parent;
				_adapter = adapter;
				Type = type;
				Name = type.GetDisplayName();
			}

			public MessageTypes Type { get; }
			public string Name { get; private set; }

			public bool IsSelected
			{
				get { return _adapter.IsMessageSupported(Type); }
				set
				{
					if (value)
						_adapter.AddSupportedMessage(Type);
					else
						_adapter.RemoveSupportedMessage(Type);

					_parent.SelectedChanged.SafeInvoke();
				}
			}
		}

		private readonly ObservableCollection<SupportedMessage> _supportedMessages = new ObservableCollection<SupportedMessage>();
		private IMessageAdapter _adapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectorSupportedMessagesPanel"/>.
		/// </summary>
		public ConnectorSupportedMessagesPanel()
		{
			InitializeComponent();

			ItemsSource = _supportedMessages;
		}

		/// <summary>
		/// The message adapter.
		/// </summary>
		public IMessageAdapter Adapter
		{
			get { return _adapter; }
			set
			{
				if (_adapter == value)
					return;

				_adapter = value;

				_supportedMessages.Clear();

				if (_adapter == null)
					return;

				var types = Enumerator.GetValues<MessageTypes>().ToHashSet();

				var message = _adapter.GetType()
					.CreateInstance<IMessageAdapter>(_adapter.TransactionIdGenerator)
					.SupportedMessages
					.Where(m => types.Contains(m))
					.Select(m => new SupportedMessage(this, _adapter, m))
					.ToArray();

				_supportedMessages.AddRange(message);
			}
		}

		internal event Action SelectedChanged;
	}
}