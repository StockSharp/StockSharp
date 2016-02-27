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

using System.Collections.Generic;

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

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(BaseStudioControl),
			new PropertyMetadata(string.Empty));

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Uri), typeof(BaseStudioControl),
			new PropertyMetadata(null));

		public Uri Icon
		{
			get { return (Uri)GetValue(IconProperty); }
			set { SetValue(IconProperty, value); }
		}

		public virtual string Key {get; set;} = $"_{Guid.NewGuid().ToString("N")}";

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
			Title = storage.GetValue<string>(nameof(Title));
			//Icon = storage.GetValue<Uri>(nameof(Icon));
			Key = storage.GetValue<string>(nameof(Key));
		}

		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Title), Title);
			//storage.SetValue(nameof(Icon), Icon);
			storage.SetValue(nameof(Key), Key);
		}

		public virtual void Dispose()
		{
		}

		protected void RaiseChanged()
		{
			Changed.SafeInvoke(this);
		}

		#region INotifyPropertyChanged

		/// <summary>
		/// Property value change event.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// To call the event <see cref="PropertyChanged"/>.
		/// </summary>
		/// <param name="name">Property name.</param>
		protected void RaisePropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>
		/// Update field value and raise PropertyChanged event.
		/// </summary>
		/// <param name="field">Field to update.</param>
		/// <param name="value">New value.</param>
		/// <param name="name">Name of the field to update.</param>
		/// <typeparam name="T">The field type.</typeparam>
		/// <returns>True if the field was updated. False otherwise.</returns>
		protected bool SetField<T>(ref T field, T value, string name) {
			if(EqualityComparer<T>.Default.Equals(field, value))
				return false;
			
			field = value;

			RaisePropertyChanged(name);

			return true;
		}

		#endregion
	}
}