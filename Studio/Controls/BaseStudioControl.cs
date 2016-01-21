#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: BaseStudioControl.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(BaseStudioControl),
			new PropertyMetadata(string.Empty));

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Uri), typeof(BaseStudioControl),
			new PropertyMetadata(null));

		public Uri Icon
		{
			get { return (Uri)GetValue(IconProperty); }
			set { SetValue(IconProperty, value); }
		}

		public virtual string Key => Guid.NewGuid().ToString();

		// TODO change to ControlChangedCommand
		public event Action<BaseStudioControl> Changed;

		protected BaseStudioControl()
		{
			var type = GetType();

			Title = type.GetDisplayName();
			Icon = type.GetIconUrl();
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

		public virtual bool CanClose()
		{
			return true;
		}

		public virtual void Load(SettingsStorage storage)
		{
		}

		public virtual void Save(SettingsStorage storage)
		{
		}

		public virtual void Dispose()
		{
		}

		protected void RaiseChanged()
		{
			Changed.SafeInvoke(this);
		}

		#region INotifyPropertyChanged

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		#endregion
	}
}