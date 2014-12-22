namespace StockSharp.Studio.Controls.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	using StockSharp.Community;

	class HierarchyConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var id = (long)values[1];

			var s = new CollectionViewSource
			{
				Source = values[0],
			};
			s.View.Filter += obj =>
			{
				var room = obj as RoomItem;
				return room != null && room.Room.ParentRoomId == id;
			};

			return s.View;
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class RootHierarchyConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var s = new CollectionViewSource
			{
				Source = value,
			};
			s.View.Filter += obj =>
			{
				var room = obj as RoomItem;
				return room != null && room.Room.ParentRoomId == null;
			};

			return s.View;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class UserNameConverter : DependencyObject, IValueConverter
	{
		public static ChatClient ChatClient { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var userId = (long)value;
			return userId == 0 ? string.Empty : ChatClient.GetUser(userId).Name;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
