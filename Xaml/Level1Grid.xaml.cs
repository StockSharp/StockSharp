#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: Level1Grid.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		public IListEx<Level1ChangeMessage> Messages => _messages;

		/// <summary>
		/// The selected message.
		/// </summary>
		public Level1ChangeMessage SelectedMessage => SelectedMessages.FirstOrDefault();

		/// <summary>
		/// Selected messages.
		/// </summary>
		public IEnumerable<Level1ChangeMessage> SelectedMessages => SelectedItems.Cast<Level1ChangeMessage>();
	}
}