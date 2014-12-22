namespace StockSharp.Studio.Controls
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Studio.Core;

	public abstract class BaseStudioControl : UserControl, IStudioControl, INotifyPropertyChanged
	{
		private Action _loadedAction;

		protected BaseStudioControl()
		{
			var type = GetType();

			Title = type.GetDisplayName();

			var iconAttr = type.GetAttribute<IconAttribute>();
			Icon = iconAttr == null ? null : iconAttr.GetResourceUrl(type);
		}

		#region OnLoaded

		protected void WhenLoaded(Action action)
		{
			_loadedAction = action;
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;
			_loadedAction.SafeInvoke();
		}

		#endregion

		public virtual void Load(SettingsStorage storage)
		{
		}

		public virtual void Save(SettingsStorage storage)
		{
		}

		public virtual void Dispose()
		{
		}

		private string _title;

		public string Title
		{
			get { return _title; }
			protected set
			{
				_title = value;
				_propertyChanged.SafeInvoke(this, "Title");
			}
		}

		public Uri Icon { get; protected set; }

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}
	}

	public class IconAttribute : Attribute
	{
		public string Path { get; private set; }
		public bool IsFullPath { get; private set; }

		public IconAttribute(string path, bool isFullPath = false)
		{
			if (path.IsEmpty())
				throw new ArgumentNullException("path");

			Path = path;
			IsFullPath = isFullPath;
		}

		public Uri GetResourceUrl(Type type)
		{
			return IsFullPath ? new Uri(Path, UriKind.Relative) : Path.GetResourceUrl(type);
		}
	}
}