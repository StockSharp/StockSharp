namespace SampleDiagram.Layout
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Xaml;

	public class DockingControl : UserControl, IPersistable
	{
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(DockingControl),
			new PropertyMetadata(string.Empty));

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Uri), typeof(DockingControl),
			new PropertyMetadata(null));

		public Uri Icon
		{
			get { return (Uri)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public virtual object Key => Guid.NewGuid();

		public DockingControl()
		{
			var type = GetType();

			Title = type.GetDisplayName();
			Icon = type.GetIconUrl();
		}

		public virtual void Load(SettingsStorage storage)
		{
		}

		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("ControlType", GetType().GetTypeName(false));
		}
	}
}
