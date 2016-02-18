#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.Layout.SampleDiagramPublic
File: LayoutManager.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System.Windows;

namespace StockSharp.Terminal.Layout
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Studio.Controls;

	using DevExpress.Xpf.Docking;

	public sealed class LayoutManager : BaseLogReceiver
	{
		private readonly Dictionary<string, LayoutPanel> _panels = new Dictionary<string, LayoutPanel>();
		private readonly SynchronizedDictionary<string, BaseStudioControl> _controlsDict = new SynchronizedDictionary<string, BaseStudioControl>();

		private readonly object _syncRoot = new object();

		//private LayoutPanel RootGroup => DockCtl.LayoutRoot;

		private DockLayoutManager DockCtl { get; }

//		public IEnumerable<BaseStudioControl> DockingControls
//		{
//			get
//			{
//				return DockCtl
//					.Layout
//					.Descendents()
//					.OfType<LayoutContent>()
//					.Select(c => c.Content)
//					.OfType<BaseStudioControl>()
//					.ToArray();
//			}
//		}

		public LayoutManager(DockLayoutManager dockCtl)
		{
			if (dockCtl == null)
				throw new ArgumentNullException(nameof(dockCtl));

			DockCtl = dockCtl;
		}

		public void OpenToolWindow(BaseStudioControl content, bool canClose = true)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var item = _panels.TryGetValue(content.Key);

			if (item == null)
			{
				item = CreatePanel(content.Key, string.Empty, content, canClose);
				item.SetBindings(BaseLayoutItem.CaptionProperty, content, "Title");

				_panels.Add(content.Key, item);
				_controlsDict[content.Key] = content;

				//DockCtl.LayoutRoot.Add(item);
			}

			DockCtl.ActiveLayoutItem = item;
		}

		private LayoutPanel CreatePanel(string key, string title, object content, bool canClose = true)
		{
			var panel = DockCtl.DockController.AddPanel(new Point(100, 100), new Size(400, 300));

			panel.Name = key;
			panel.Caption = title;
			panel.Content = content;
			panel.AllowClose = canClose;

			return panel;
		}

		public override void Load(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			_panels.Clear();
			_controlsDict.Clear();

			var controls = storage.GetValue<SettingsStorage[]>("Controls");

			foreach (var settings in controls)
			{
				try
				{
					var control = LoadBaseStudioControl(settings);
					_controlsDict.Add(control.Key, control);
				}
				catch (Exception excp)
				{
					this.AddErrorLog(excp);
				}
			}

			var layout = storage.GetValue<string>("Layout");
				
			if (!layout.IsEmpty())
				LoadLayout(layout);
		}

		public override void Save(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			storage.SetValue("Controls", _controlsDict.Values.Select(SaveControl).ToArray());

			var layout = SaveLayout();
			storage.SetValue("Layout", layout);
		}

		private void LoadLayout(string settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			try
			{
				_panels.Clear();

				DockCtl.RestoreLayoutFromXml("docklayout.xml");

				//using(var stream = new MemoryStream(settings.Base64()))
				//	DockCtl.RestoreLayoutFromStream(stream);

//				using (var reader = new StringReader(settings))
//				{
//					var layoutSerializer = new XmlLayoutSerializer(DockCtl);
//					layoutSerializer.LayoutSerializationCallback += (s, e) =>
//					{
//						var control = _controlsDict.TryGetValue(e.Model.ContentId);
//						if (control == null)
//						{
//							e.Model.Close();
//							return;
//						}
//
//						e.Content = control;
//						e.Model.SetBindings(LayoutContent.TitleProperty, control, "Title");
//						_panels[control.Key] = e.Model;
//					};
//
//					layoutSerializer.Deserialize(reader);
//				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, LocalizedStrings.Str3649);
			}
		}

		public string SaveLayout()
		{
			DockCtl.SaveLayoutToXml("docklayout.xml");

			return string.Empty;

//			using (var stream = new MemoryStream())
//			{
//				DockCtl.SaveLayoutToStream(stream);
//				stream.Position = 0;
//
//				return stream.ReadBuffer().Base64();
//			}
		}

		private SettingsStorage SaveControl(BaseStudioControl control)
		{
			var storage = new SettingsStorage();

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				storage.SetValue("ControlType", control.GetType().GetTypeName(false));
				control.Save(storage);
			});

			return storage;
		}

		private static BaseStudioControl LoadBaseStudioControl(SettingsStorage settings)
		{
			var type = settings.GetValue<Type>("ControlType");
			var control = (BaseStudioControl)Activator.CreateInstance(type);

			control.Load(settings);

			return control;
		}
	}
}
