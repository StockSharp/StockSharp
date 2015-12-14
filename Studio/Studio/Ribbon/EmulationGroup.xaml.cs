#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: EmulationGroup.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System.Windows;

	using Ecng.Xaml;

	using StockSharp.Studio.Services;

	public partial class EmulationGroup
	{
		public static readonly DependencyProperty EmulationServiceProperty = DependencyProperty.Register("EmulationService", typeof(EmulationService), typeof(EmulationGroup),
			new FrameworkPropertyMetadata(null, OnEmulationServicePropertyChanged));

		public EmulationService EmulationService
		{
			get { return (EmulationService)GetValue(EmulationServiceProperty); }
			set { SetValue(EmulationServiceProperty, value); }
		}

		private static void OnEmulationServicePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = (EmulationGroup)source;

			var oldValue = (EmulationService)e.OldValue;
			var newValue = (EmulationService)e.NewValue;

			if (ctrl._token == null && newValue != null)
			{
				ctrl._token = GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(() =>
				{
					if (ctrl.EmulationService == null || !ctrl.EmulationService.IsInProgress)
						return;

					ctrl.EmulationService.RefreshStatistics();
				});
			}

			if (oldValue != null && newValue == null && ctrl._token != null)
			{
				GuiDispatcher.GlobalDispatcher.RemovePeriodicalAction(ctrl._token);
				ctrl._token = null;
			}
		}

		private object _token;

		public EmulationGroup()
		{
			InitializeComponent();
		}
	}
}