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

	using Xceed.Wpf.AvalonDock;
	using Xceed.Wpf.AvalonDock.Layout;
	using Xceed.Wpf.AvalonDock.Layout.Serialization;

	public sealed class LayoutManager : BaseLogReceiver
	{
		private readonly Dictionary<string, LayoutContent> _anchorables = new Dictionary<string, LayoutContent>();
		private readonly SynchronizedDictionary<string, BaseStudioControl> _controlsDict = new SynchronizedDictionary<string, BaseStudioControl>();

		private readonly object _syncRoot = new object();

		private LayoutPanel RootGroup => DockingManager.Layout.RootPanel;

		private DockingManager DockingManager { get; }

		public IEnumerable<BaseStudioControl> DockingControls
		{
			get
			{
				return DockingManager
					.Layout
					.Descendents()
					.OfType<LayoutContent>()
					.Select(c => c.Content)
					.OfType<BaseStudioControl>()
					.ToArray();
			}
		}

		public event Action Changed; 

		public LayoutManager(DockingManager dockingManager)
		{
			if (dockingManager == null)
				throw new ArgumentNullException(nameof(dockingManager));

			DockingManager = dockingManager;
		}

		public void OpenToolWindow(BaseStudioControl content, bool canClose = true)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var anchorable = _anchorables.TryGetValue(content.Key);

			if (anchorable == null)
			{
				anchorable = CreateAnchorable(content.Key, string.Empty, content, canClose);
				anchorable.SetBindings(LayoutContent.TitleProperty, content, "Title");

				_anchorables.Add(content.Key, anchorable);
				_controlsDict[content.Key] = content;
			
				RootGroup.Children.Add(new LayoutAnchorablePane((LayoutAnchorable) anchorable));

				anchorable.Float();
			}

			DockingManager.ActiveContent = anchorable.Content;
		}

		private LayoutAnchorable CreateAnchorable(string key, string title, object content, bool canClose = true)
		{
			return new LayoutAnchorable
			{
				ContentId = key,
				Title = title,
				FloatingTop = 100,
				FloatingLeft = 100,
				FloatingWidth = 500,
				FloatingHeight = 350,
				Content = content,
				CanClose = canClose
			};
		}

		public override void Load(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			_anchorables.Clear();
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

			var layout = Encoding.UTF8.GetString(storage.GetValue<string>("Layout").Base64());
				
			if (!layout.IsEmpty())
				LoadLayout(layout);
		}

		public override void Save(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			storage.SetValue("Controls", _controlsDict.Values.Select(SaveControl).ToArray());

			var layout = Encoding.UTF8.GetBytes(SaveLayout()).Base64();
			storage.SetValue("Layout", layout);
		}

		private void LoadLayout(string settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			try
			{
				_anchorables.Clear();

				using (var reader = new StringReader(settings))
				{
					var layoutSerializer = new XmlLayoutSerializer(DockingManager);
					layoutSerializer.LayoutSerializationCallback += (s, e) =>
					{
						var control = _controlsDict.TryGetValue(e.Model.ContentId);
						if (control == null)
						{
							e.Model.Close();
							return;
						}

						e.Content = control;
						e.Model.SetBindings(LayoutContent.TitleProperty, control, "Title");
						_anchorables[control.Key] = e.Model;
					};

					layoutSerializer.Deserialize(reader);
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, LocalizedStrings.Str3649);
			}
		}

		public string SaveLayout()
		{
			var builder = new StringBuilder();

			try
			{
				using (var writer = new StringWriter(builder))
				{
					new XmlLayoutSerializer(DockingManager).Serialize(writer);
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, LocalizedStrings.Str3649);
			}

			return builder.ToString();
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
