#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Controls.HydraPublic
File: ImportEnumMappingButton.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Controls
{
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Hydra.Windows;

	public partial class ImportEnumMappingButton
	{
		public ImportEnumMappingButton()
		{
			InitializeComponent();
		}

		public static DependencyProperty EnumTypeProperty = DependencyProperty.Register(nameof(EnumType), typeof(Type), typeof(ImportEnumMappingButton), new PropertyMetadata((s, e) =>
		{
			var ctrl = s as ImportEnumMappingButton;
			
			if (ctrl == null)
				return;

			ctrl.IsEnabled = ((Type)e.NewValue).IsEnum;
		}));

		public Type EnumType
		{
			get { return (Type)GetValue(EnumTypeProperty); }
			set { SetValue(EnumTypeProperty, value); }
		}

		public static DependencyProperty ValuesProperty = DependencyProperty.Register(nameof(Values), typeof(ObservableCollection<ImportEnumMappingWindow.MappingValue>), typeof(ImportEnumMappingButton), new PropertyMetadata((s, e) =>
		{
			var ctrl = s as ImportEnumMappingButton;

			if (ctrl == null)
				return;

			//var values = (ObservableCollection<ImportEnumMappingWindow.MappingValue>)e.NewValue;
			ctrl.RefreshTitle();
		}));

		private void RefreshTitle()
		{
			Content = Values.IsEmpty() ? "..." : Values.Select(v => "{0} -> {1}".Put(v.ValueFile, v.ValueStockSharp.GetDisplayName())).Join(" ");
		}

		public ObservableCollection<ImportEnumMappingWindow.MappingValue> Values
		{
			get { return (ObservableCollection<ImportEnumMappingWindow.MappingValue>)GetValue(ValuesProperty); }
			set { SetValue(ValuesProperty, value); }
		}

		public static DependencyProperty FieldNameProperty = DependencyProperty.Register(nameof(FieldName), typeof(string), typeof(ImportEnumMappingButton));

		public string FieldName
		{
			get { return (string)GetValue(FieldNameProperty); }
			set { SetValue(FieldNameProperty, value); }
		}

		protected override void OnClick()
		{
			base.OnClick();

			var wnd = new ImportEnumMappingWindow { EnumType = EnumType };

			wnd.Values.AddRange(Values.Select(v => v.Clone()));
			wnd.Title = wnd.Title.Put(FieldName);

			if (!wnd.ShowModal(this))
				return;

			Values.Clear();
			Values.AddRange(wnd.Values);

			RefreshTitle();
		}
	}
}