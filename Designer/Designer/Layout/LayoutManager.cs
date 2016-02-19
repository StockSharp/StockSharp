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
namespace StockSharp.Designer.Layout
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;

	using DevExpress.Xpf.Docking;
	using DevExpress.Xpf.Docking.Base;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Studio.Controls;

	public sealed class LayoutManager : BaseLogReceiver
	{
		private readonly DocumentGroup _documentGroup;
		private readonly Dictionary<string, DocumentPanel> _documents = new Dictionary<string, DocumentPanel>();
		//private readonly Dictionary<string, LayoutAnchorable> _anchorables = new Dictionary<string, LayoutAnchorable>();
		private readonly List<BaseStudioControl> _controls = new List<BaseStudioControl>();

		private readonly SynchronizedDictionary<BaseStudioControl, SettingsStorage> _dockingControlSettings = new SynchronizedDictionary<BaseStudioControl, SettingsStorage>();
		private readonly SynchronizedSet<BaseStudioControl> _changedControls = new CachedSynchronizedSet<BaseStudioControl>();

		private readonly TimeSpan _period = TimeSpan.FromSeconds(5);
		private readonly object _syncRoot = new object();

		private Timer _flushTimer;
		private bool _isFlushing;
		private bool _isLayoutChanged;
		private bool _isDisposing;
		private bool _isSettingsChanged;
		private string _layout;

		public DockLayoutManager DockingManager { get; }

		public IEnumerable<BaseStudioControl> DockingControls => _controls.ToArray();

		public event Action Changed; 

		public LayoutManager(DockLayoutManager dockingManager, DocumentGroup documentGroup = null)
		{
			if (dockingManager == null)
				throw new ArgumentNullException(nameof(dockingManager));

			_documentGroup = documentGroup;

			DockingManager = dockingManager;

			DockingManager.DockItemClosing += OnDockingManagerDockItemClosing;
			DockingManager.DockItemClosed += OnDockingManagerDockItemClosed;
			DockingManager.DockItemHidden += DockingManager_DockItemHidden;
			DockingManager.DockItemRestored += DockingManager_DockItemRestored;
			DockingManager.DockItemExpanded += DockingManager_DockItemExpanded;
			DockingManager.DockItemActivated += DockingManager_DockItemActivated;
			DockingManager.DockItemEndDocking += DockingManager_DockItemEndDocking;

			DockingManager.LayoutItemSizeChanged += DockingManager_LayoutItemSizeChanged;
		}

		//public void OpenToolWindow(BaseStudioControl content, bool canClose = true)
		//{
		//	if (content == null)
		//		throw new ArgumentNullException(nameof(content));

		//	var anchorable = _anchorables.TryGetValue(content.Key);

		//	if (anchorable == null)
		//	{
		//		content.Changed += OnBaseStudioControlChanged;

		//		anchorable = new LayoutAnchorable
		//		{
		//			ContentId = content.Key,
		//			Content = content,
		//			CanClose = canClose
		//		};

		//		anchorable.SetBindings(LayoutContent.TitleProperty, content, "Title");

		//		_anchorables.Add(content.Key, anchorable);

		//		//RootGroup.Children.Add(new LayoutAnchorablePane(anchorable));
		//		OnBaseStudioControlChanged(content);
		//	}

		//	//DockingManager.ActiveContent = anchorable.Content;
		//}

		public void OpenDocumentWindow(BaseStudioControl content, bool canClose = true)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var document = _documents.TryGetValue(content.Key);

			if (document == null)
			{
				content.Changed += OnBaseStudioControlChanged;

				document = DockingManager.DockController.AddDocumentPanel(_documentGroup);
				document.Name = "_" + content.Key.Replace("-", "_");
				document.Content = content;
				document.ShowCloseButton = canClose;

				document.SetBindings(BaseLayoutItem.CaptionProperty, content, "Title");

				_documents.Add(content.Key, document);
				_controls.Add(content);

				OnBaseStudioControlChanged(content);
			}

			DockingManager.ActiveLayoutItem = document;
		}

		public void CloseDocumentWindow(BaseStudioControl content)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var document = _documents.TryGetValue(content.Key);

			if (document == null)
				return;

			DockingManager.DockController.Close(document);
		}

		public override void Load(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			_documents.Clear();
			//_anchorables.Clear();
			_changedControls.Clear();
			_dockingControlSettings.Clear();

			var controls = storage.GetValue<SettingsStorage[]>("Controls");

			foreach (var settings in controls)
			{
				try
				{
					var control = LoadBaseStudioControl(settings);

					_dockingControlSettings.Add(control, settings);
					OpenDocumentWindow(control);
				}
				catch (Exception excp)
				{
					this.AddErrorLog(excp);
				}
			}

			_layout = storage.GetValue<string>("Layout");

			if (!_layout.IsEmpty())
				LoadLayout(_layout);
		}

		public override void Save(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			storage.SetValue("Controls", _dockingControlSettings.SyncGet(c => c.Select(p => p.Value).ToArray()));
			storage.SetValue("Layout", _layout);
		}

		public void LoadLayout(string settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			try
			{
				var data = Encoding.UTF8.GetBytes(settings);

                using (var stream = new MemoryStream(data))
					DockingManager.RestoreLayoutFromStream(stream);
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, LocalizedStrings.Str3649);
			}
		}

		public string SaveLayout()
		{
			var layout = string.Empty;

			try
			{
				using (var stream = new MemoryStream())
				{
					DockingManager.SaveLayoutToStream(stream);
					layout = Encoding.UTF8.GetString(stream.ToArray());
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, LocalizedStrings.Str3649);
			}

			return layout;
		}

		public void FlushSettings()
		{
			_isSettingsChanged = true;
			Flush();
		}

		protected override void DisposeManaged()
		{
			_isDisposing = true;

			Save(DockingControls, true);
			Changed.SafeInvoke();

			base.DisposeManaged();
		}

		private void OnDockingManagerDockItemClosing(object sender, ItemCancelEventArgs e)
		{
			var control = ((DocumentPanel)e.Item).Content as BaseStudioControl;

			if (control == null)
				return;

			e.Cancel = !control.CanClose();
		}

		private void OnDockingManagerDockItemClosed(object sender, DockItemClosedEventArgs e)
		{
			var panel = (DocumentPanel)e.Item;
            var control = panel.Content as BaseStudioControl;

			if (control == null)
				return;

			_documents.RemoveWhere(p => Equals(p.Value, panel));
			_controls.Remove(control);

			_isLayoutChanged = true;

			_changedControls.Remove(control);
			_dockingControlSettings.Remove(control);

			Flush();
		}

		private void DockingManager_DockItemEndDocking(object sender, DockItemDockingEventArgs e)
		{
			OnDickingChanged();
		}

		private void DockingManager_DockItemActivated(object sender, DockItemActivatedEventArgs ea)
		{
			OnDickingChanged();
		}

		private void DockingManager_DockItemExpanded(object sender, DockItemExpandedEventArgs e)
		{
			OnDickingChanged();
		}

		private void DockingManager_DockItemRestored(object sender, ItemEventArgs e)
		{
			OnDickingChanged();
		}

		private void DockingManager_DockItemHidden(object sender, ItemEventArgs e)
		{
			OnDickingChanged();
		}

		private void DockingManager_LayoutItemSizeChanged(object sender, LayoutItemSizeChangedEventArgs e)
		{
			OnDickingChanged();
		}

		private void OnDickingChanged()
		{
			_isLayoutChanged = true;
			Flush();
		}

		private void OnBaseStudioControlChanged(BaseStudioControl control)
		{
			_changedControls.Add(control);
			Flush();
		}

		private void Flush()
		{
			lock (_syncRoot)
			{
				if (_isFlushing || _flushTimer != null)
					return;

				_flushTimer = new Timer(OnFlush, null, _period, _period);
			}
		}

		private void OnFlush(object state)
		{
			BaseStudioControl[] items;
			bool isLayoutChanged;
			bool isSettingsChanged;

			lock (_syncRoot)
			{
				if (_isFlushing || _isDisposing)
					return;

				isLayoutChanged = _isLayoutChanged;
				isSettingsChanged = _isSettingsChanged;
				items = _changedControls.CopyAndClear();

				_isFlushing = true;
				_isLayoutChanged = false;
				_isSettingsChanged = false;
			}

			try
			{
				var needSave = items.Length > 0 || isLayoutChanged;

                if (needSave || isSettingsChanged)
				{
					if (needSave)
					{
						GuiDispatcher.GlobalDispatcher.AddSyncAction(() =>
						{
							if (_isDisposing)
								return;

							Save(items, isLayoutChanged);
						});
					}

					Changed.SafeInvoke();
				}
				else
				{
					lock (_syncRoot)
					{
						if (_flushTimer == null)
							return;

						_flushTimer.Dispose();
						_flushTimer = null;
					}
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, "Flush layout changed error.");
			}
			finally
			{
				lock (_syncRoot)
					_isFlushing = false;
			}
		}

		private void Save(IEnumerable<BaseStudioControl> items, bool isLayoutChanged)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				foreach (var control in items)
				{
					var storage = new SettingsStorage();
					storage.SetValue("ControlType", control.GetType().GetTypeName(false));
					control.Save(storage);

					_dockingControlSettings[control] = storage;
				}

				if (isLayoutChanged)
					_layout = SaveLayout();
			});
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
