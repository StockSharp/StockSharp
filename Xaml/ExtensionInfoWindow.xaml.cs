namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;
	using System.ComponentModel;
	using System.Dynamic;

	using Ecng.Common;

	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Окно для редактирования расширенной информации <see cref="IExtendableEntity.ExtensionInfo"/>.
	/// </summary>
	public partial class ExtensionInfoWindow
	{
		/// <summary>
		/// Создать <see cref="ExtensionInfoWindow"/>.
		/// </summary>
		public ExtensionInfoWindow()
		{
			InitializeComponent();
		}

		private IDictionary<object, object> _data;

		/// <summary>
		/// Расширенная информация.
		/// </summary>
		public IDictionary<object, object> Data
		{
			private get { return _data; }
			set
			{
				_data = value;
				var ef = new ExtensionInfoField(value);
				ef.PropertyChanged += UpdateSecondPart;
				PropGrid.DataContext = ef;
				PropGrid.SelectedObject = ef;
				MainView.ItemsSource = value;
			}
		}

		private void UpdateSecondPart(object sender, PropertyChangedEventArgs e)
		{
			MainView.ItemsSource = null;
			MainView.ItemsSource = Data;
		}

		//public event Action RemoveItem;

		private void Delete_Button_Click(object sender, RoutedEventArgs e)
		{
			var btn = (Button)sender;
			KeyValuePair<object, object> dvp;
			if (btn.DataContext is KeyValuePair<object, object>)
			{
				dvp = (KeyValuePair<object, object>)btn.DataContext;
			}
			else
			{
				dvp = new KeyValuePair<object, object>();
			}

			Data.Remove(dvp.Key);

			//if (RemoveItem != null)
			//{
			//    RemoveItem();
			//}

			var ef = new ExtensionInfoField(Data);
			PropGrid.DataContext = ef;
			PropGrid.SelectedObject = ef;

			MainView.ItemsSource = null;
			MainView.ItemsSource = Data;
		}

		[TypeConverter(typeof(CustomObjectConverter))]
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		private sealed class ExtensionInfoField : DynamicObject, INotifyPropertyChanged
		{
			public ExtensionInfoField(IDictionary<object, object> extensionInfo)
			{
				Data = extensionInfo;
			}

			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				object val;

				if (Data != null && Data.TryGetValue(binder.Name, out val))
				{
					result = val;
					return true;
				}
				else
				{
					result = null;
					return false;
				}
			}

			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				object val;

				if (Data.TryGetValue(binder.Name, out val))
				{
					Data[binder.Name] = value.ToString();
				}
				else
				{
					Data.Add(binder.Name, value.ToString());
				}

				NotifyPropertyChanged(binder.Name);
				return true;
			}

			private object this[string name]
			{
				get { return Data[name]; }
				set
				{
					object val;

					if (Data.TryGetValue(name, out val))
					{
						Data[name] = value;
					}
					else
					{
						Data.Add(name, value);
					}

					NotifyPropertyChanged(name);
				}
			}

			[Browsable(false)]
			public IDictionary<object, object> Data { get; private set; }

			private sealed class CustomObjectConverter : ExpandableObjectConverter
			{
				public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
				{
					var stdProps = base.GetProperties(context, value, attributes);
					var obj = value as ExtensionInfoField;
					var customProps = obj == null ? null : obj.Data;

					var props = new PropertyDescriptor[stdProps.Count + (customProps == null ? 0 : customProps.Count)];
					stdProps.CopyTo(props, 0);

					if (customProps != null)
					{
						int index = stdProps.Count;
						foreach (var prop in customProps)
						{
							var pn = new KeyValuePair<string, object>(prop.Key.ToString(), prop.Value);
							var cpd = new CustomPropertyDescriptor(pn);

							props[index++] = cpd;
						}
					}

					return new PropertyDescriptorCollection(props);
				}
			}

			private sealed class CustomPropertyDescriptor : PropertyDescriptor
			{
				private readonly KeyValuePair<string, object> _prop;

				public CustomPropertyDescriptor(KeyValuePair<string, object> prop)
					: base(prop.Key, null)
				{
					_prop = prop;
				}

				public override string Category
				{
					get { return string.Empty; }
				}

				public override string Description
				{
					get { return _prop.Key; }
				}

				public override string Name
				{
					get { return _prop.Key; }
				}

				public override bool ShouldSerializeValue(object component)
				{
					return true;
				}

				public override void ResetValue(object component)
				{
				}

				public override bool IsReadOnly
				{
					get { return false; }
				}

				public override Type PropertyType
				{
					get { return _prop.Value.GetType(); }
				}

				public override bool CanResetValue(object component)
				{
					return false;
				}

				public override Type ComponentType
				{
					get { return typeof(ExtensionInfoField); }
				}

				public override void SetValue(object component, object value)
				{
					((ExtensionInfoField)component)[_prop.Key] = value;
				}

				public override object GetValue(object component)
				{
					return ((ExtensionInfoField)component)[_prop.Key];
				}

			}

			private void NotifyPropertyChanged(string name)
			{
				PropertyChanged.SafeInvoke(this, name);
			}

			public event PropertyChangedEventHandler PropertyChanged;
		}
	}
}