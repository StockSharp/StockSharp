namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Messages;

	/// <summary>
	/// Таблица для отображения сообщения <see cref="Level1ChangeMessage"/>.
	/// </summary>
	public partial class Level1Grid
	{
		private readonly ThreadSafeObservableCollection<Level1ChangeMessage> _messages;

		/// <summary>
		/// Создать <see cref="Level1Grid"/>.
		/// </summary>
		public Level1Grid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<Level1ChangeMessage>();
			ItemsSource = itemsSource;

			_messages = new ThreadSafeObservableCollection<Level1ChangeMessage>(itemsSource) { MaxCount = 10000 };
		}

		/// <summary>
		/// Максимальное число сообщений для показа. Значение -1 означает бесконечное количество.
		/// По-умолчанию равно 10000.
		/// </summary>
		public int MaxCount
		{
			get { return _messages.MaxCount; }
			set { _messages.MaxCount = value; }
		}

		/// <summary>
		/// Список сообщений, добавленных в таблицу.
		/// </summary>
		public IListEx<Level1ChangeMessage> Messages
		{
			get { return _messages; }
		}

		/// <summary>
		/// Выбранное сообщение.
		/// </summary>
		public Level1ChangeMessage SelectedMessage
		{
			get { return SelectedMessages.FirstOrDefault(); }
		}

		/// <summary>
		/// Выбранные сообщения.
		/// </summary>
		public IEnumerable<Level1ChangeMessage> SelectedMessages
		{
			get { return SelectedItems.Cast<Level1ChangeMessage>(); }
		}
	}
}