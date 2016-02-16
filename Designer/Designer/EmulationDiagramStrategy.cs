#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: EmulationDiagramStrategy.cs
Created: 2015, 12, 9, 6:53 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Designer
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Xaml.Diagram;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	public enum MarketDataSource
	{
		Ticks,
		Candles
	}

	public class EmulationDiagramStrategy : DiagramStrategy
	{
		private EmulationSettings _emulationSettings;

		[DisplayNameLoc(LocalizedStrings.SettingsKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[DescriptionLoc(LocalizedStrings.Str1408Key)]
		[PropertyOrder(1)]
		public EmulationSettings EmulationSettings
		{
			get { return _emulationSettings; }
			set
			{
				if (_emulationSettings == value)
					return;

				if (_emulationSettings != null)
					_emulationSettings.PropertyChanged -= OnEmulationSettingsPropertyChanged;

				_emulationSettings = value;

				if (_emulationSettings == null)
					return;

				_emulationSettings.PropertyChanged += OnEmulationSettingsPropertyChanged;
			}
		}

		public EmulationDiagramStrategy()
		{
			EmulationSettings = new EmulationSettings();
		}

		private void OnEmulationSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			RaiseParametersChanged(e.PropertyName);
		}

		protected override bool NeedShowProperty(PropertyDescriptor propertyDescriptor)
		{
			return propertyDescriptor.DisplayName != LocalizedStrings.Portfolio && base.NeedShowProperty(propertyDescriptor);
		}

		public override void Load(SettingsStorage storage)
		{
			var compositionId = storage.GetValue<Guid>("CompositionId");
			var registry = ConfigManager.GetService<StrategiesRegistry>();
			var composition = (CompositionDiagramElement)registry.Strategies.FirstOrDefault(c => c.TypeId == compositionId);

			Composition = registry.Clone(composition);
			Id = storage.GetValue<Guid>("StrategyId");

			var emulationSettings = storage.GetValue<SettingsStorage>("EmulationSettings");

			if (emulationSettings != null)
				EmulationSettings.Load(emulationSettings);

			base.Load(storage);
		}

		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("StrategyId", Id);
			storage.SetValue("CompositionId", Composition.TypeId);

			storage.SetValue("EmulationSettings", EmulationSettings.Save());

			base.Save(storage);
		}
	}
}