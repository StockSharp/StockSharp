#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: EmulationControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System;
using Ecng.ComponentModel;
using Ecng.Configuration;
using Ecng.Serialization;
using StockSharp.Terminal.Layout;
using StockSharp.BusinessEntities;
using StockSharp.Studio.Controls;
using StockSharp.Studio.Core.Commands;

namespace StockSharp.Terminal.Controls
{
	public partial class WorkAreaControl
	{
		private readonly LayoutManager _layoutManager;
		private Security _lastSelectedSecurity;

		public WorkAreaControl()
		{
			InitializeCommands();
			InitializeComponent();

			_layoutManager = new LayoutManager(DockManager);
		}

		private void InitializeCommands()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			cmdSvc.Register<SelectCommand>(this, true, cmd =>
			{
				var sec = cmd.Instance as Security;
				if(sec == null)
					return;

				_lastSelectedSecurity = sec;
			});
		}

		public void HandleNewPanelSelection(Type controlType)
		{
			if(controlType == null || !typeof(BaseStudioControl).IsAssignableFrom(controlType))
				return;

			var control = (BaseStudioControl) Activator.CreateInstance(controlType);

			var depthControl = control as ScalpingMarketDepthControl;
			if (depthControl != null && _lastSelectedSecurity != null)
				depthControl.Settings.Security = _lastSelectedSecurity;

			control.Title = control.GetType().GetDisplayName();

			_layoutManager.OpenToolWindow(control);
		}

		#region IPersistable

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			_layoutManager.Load(storage.GetValue<SettingsStorage>("LayoutManager"));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("LayoutManager", _layoutManager.Save());
		}

		#endregion
	}
}
