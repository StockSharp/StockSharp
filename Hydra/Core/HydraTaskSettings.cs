#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: HydraTaskSettings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Data;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Xaml.PropertyGrid;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid;
	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// Настройки задачи <see cref="IHydraTask"/>.
	/// </summary>
	public class HydraTaskSettings : Cloneable<HydraTaskSettings>, INotifyPropertyChanged
	{
		private class DependFromComboBoxEditor : ITypeEditor
		{
			FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
			{
				var comboBox = new ComboBox
				{
					DisplayMemberPath = "Settings.Title",
					Width = double.NaN,
					ItemsSource = Extensions.Tasks.Cache
				};

				var binding = new Binding("Value")
				{
					Source = propertyItem,
					Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
				};

				BindingOperations.SetBinding(comboBox, Selector.SelectedItemProperty, binding);
				return comboBox;
			}
		}

		/// <summary>
		/// Создать <see cref="HydraTaskSettings"/>.
		/// </summary>
		public HydraTaskSettings()
		{
		}

		/// <summary>
		/// Создать <see cref="HydraTaskSettings"/>.
		/// </summary>
		/// <param name="settings">Реальные настройки.</param>
		protected HydraTaskSettings(HydraTaskSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			Securities = settings.Securities;
			Id = settings.Id;
			TaskType = settings.TaskType;
			ExtensionInfo = settings.ExtensionInfo;
		}

		/// <summary>
		/// Идентификатор задачи.
		/// </summary>
		[Identity]
		[Browsable(false)]
		public Guid Id { get; set; }

		/// <summary>
		/// Тип задачи.
		/// </summary>
		[Browsable(false)]
		//[Member(IsAssemblyQualifiedName = false)]
		public string TaskType { get; set; }

		/// <summary>
		/// Включена ли задача.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2229Key)]
		[DescriptionLoc(LocalizedStrings.Str2230Key)]
		[PropertyOrder(0)]
		[Browsable(false)]
		[Ignore]
		public bool IsEnabled
		{
			get { return (bool?)ExtensionInfo.TryGetValue("IsEnabled") ?? false; }
			set
			{
				ExtensionInfo["IsEnabled"] = value;
				NotifyPropertyChanged("IsEnabled");
			}
		}

		/// <summary>
		/// Время начала работы.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2231Key)]
		[DescriptionLoc(LocalizedStrings.Str2232Key)]
		[TimeSpan]
		[Ignore]
		[PropertyOrder(1)]
		public TimeSpan WorkingFrom
		{
			get { return (TimeSpan?)ExtensionInfo.TryGetValue("WorkingFrom") ?? TimeSpan.Zero; }
			set { ExtensionInfo["WorkingFrom"] = value; }
		}

		/// <summary>
		/// Время окончания работы.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2233Key)]
		[DescriptionLoc(LocalizedStrings.Str2234Key)]
		[TimeSpan]
		[Ignore]
		[PropertyOrder(2)]
		public TimeSpan WorkingTo
		{
			get { return (TimeSpan?)ExtensionInfo.TryGetValue("WorkingTo") ?? TimeSpan.Zero; }
			set { ExtensionInfo["WorkingTo"] = value; }
		}

		/// <summary>
		/// Интервал работы.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2235Key)]
		[DescriptionLoc(LocalizedStrings.Str2235Key, true)]
		[TimeSpan]
		[Ignore]
		[PropertyOrder(3)]
		public TimeSpan Interval
		{
			get { return (TimeSpan?)ExtensionInfo.TryGetValue("Interval") ?? TimeSpan.FromSeconds(1); }
			set { ExtensionInfo["Interval"] = value; }
		}

		/// <summary>
		/// Директория с данными, куда будут сохраняться конечные файлы в формате StockSharp.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2237Key)]
		[DescriptionLoc(LocalizedStrings.Str2238Key)]
		[Ignore]
		[PropertyOrder(4)]
		[Editor(typeof(DriveComboBoxEditor), typeof(DriveComboBoxEditor))]
		public virtual IMarketDataDrive Drive
		{
			get { return DriveCache.Instance.GetDrive((string)ExtensionInfo.TryGetValue("Drive") ?? string.Empty); }
			set { ExtensionInfo["Drive"] = value?.Path; }
		}

		/// <summary>
		/// Формат данных.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2239Key)]
		[DescriptionLoc(LocalizedStrings.Str2240Key)]
		[PropertyOrder(5)]
		[Ignore]
		public StorageFormats StorageFormat
		{
			get { return ExtensionInfo.TryGetValue("StorageFormat").To<StorageFormats?>() ?? StorageFormats.Binary; }
			set
			{
				ExtensionInfo["StorageFormat"] = value.To<string>();
				NotifyPropertyChanged("StorageFormat");
			}
		}

		/// <summary>
		/// Задача, которая должна быть выполнена перед запуском текущей.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2241Key)]
		[DescriptionLoc(LocalizedStrings.Str2242Key)]
		[PropertyOrder(10)]
		[Ignore]
		[Editor(typeof(DependFromComboBoxEditor), typeof(DependFromComboBoxEditor))]
		public IHydraTask DependFrom
		{
			get
			{
				var id = ExtensionInfo.TryGetValue("DependFrom").To<Guid?>();
				return id == null ? null : Extensions.Tasks.Cache.FirstOrDefault(t => t.Id == id);
			}
			set
			{
				if (value == null)
					ExtensionInfo["DependFrom"] = null;
				else
				{
					//if (value.Id == Id)
					//	throw new ArgumentException("Задачи '{0}' не может зависеть сама от себя.".Put(Title), "value");

					var currTask = value;

					do
					{
						if (currTask.Id == Id)
							throw new ArgumentException(LocalizedStrings.Str2243Params.Put(Title), nameof(value));

						currTask = currTask.Settings.DependFrom;
					}
					while (currTask != null);

					ExtensionInfo["DependFrom"] = value.Id;
				}
				
				NotifyPropertyChanged("DependFrom");
			}
		}

		/// <summary>
		/// Максимальное количество ошибок, после которого задача будет остановлена.
		/// По-умолчанию равно 0, что означает игнорирование количества ошибок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2244Key)]
		[DescriptionLoc(LocalizedStrings.Str2245Key)]
		[Ignore]
		[PropertyOrder(6)]
		public int MaxErrorCount
		{
			get { return (int?)ExtensionInfo.TryGetValue("MaxErrorCount") ?? 0; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				ExtensionInfo["MaxErrorCount"] = value;
			}
		}

		private bool _fieldsInitialized;
		private readonly HashSet<Level1Fields> _supportedLevel1Fields = new HashSet<Level1Fields>();

		/// <summary>
		/// Поддерживаемые поля маркет-данных первого уровня.
		/// </summary>
		[Ignore]
		[Browsable(false)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2246Key)]
		[DescriptionLoc(LocalizedStrings.Str2247Key)]
		[Editor(typeof(Level1FieldsComboBoxEditor), typeof(Level1FieldsComboBoxEditor))]
		public virtual IEnumerable<Level1Fields> SupportedLevel1Fields
		{
			get
			{
				if (!_fieldsInitialized)
				{
					var types = ExtensionInfo.TryGetValue("SupportedLevel1Fields")
						?? ExtensionInfo.TryGetValue("SupportedSecurityChangeTypes");

					_supportedLevel1Fields.AddRange(types == null
						? Level1FieldsComboBox.DefaultFields
						: ((IEnumerable<string>)types).Select(t => t.To<Level1Fields>()));

					_fieldsInitialized = true;
				}

				return _supportedLevel1Fields;
			}
			set
			{
				_supportedLevel1Fields.Clear();
				_supportedLevel1Fields.AddRange(value);

				ExtensionInfo["SupportedLevel1Fields"] = value.Select(s => s.To<string>()).ToArray();
			}
		}

		/// <summary>
		/// Настройки содержат значений, заданные по-умолчанию.
		/// </summary>
		[Ignore]
		[Browsable(false)]
		public bool IsDefault
		{
			get { return (bool?)ExtensionInfo.TryGetValue("IsDefault") ?? true; }
			set { ExtensionInfo["IsDefault"] = value; }
		}

		/// <summary>
		/// Заголовок задачи.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str215Key)]
		[DescriptionLoc(LocalizedStrings.Str2248Key)]
		[Ignore]
		[PropertyOrder(0)]
		public string Title
		{
			get { return (string)ExtensionInfo.TryGetValue("Title") ?? string.Empty; }
			set
			{
				ExtensionInfo["Title"] = value;
				NotifyPropertyChanged("Title");
			}
		}

		/// <summary>
		/// Уровень логирования.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str9Key, true)]
		[Ignore]
		[PropertyOrder(10)]
		public LogLevels LogLevel
		{
			get
			{
				var str = ExtensionInfo.TryGetValue("LogLevel");
				return str == null ? LogLevels.Inherit : str.To<LogLevels>();
			}
			set
			{
				ExtensionInfo["LogLevel"] = value.To<string>();
				NotifyPropertyChanged("LogLevel");
			}
		}

		private readonly CachedSynchronizedDictionary<object, object> _extensionInfo = new CachedSynchronizedDictionary<object, object>();

		/// <summary>
		/// Расширенная информация, храняющая в себе дополнительные настройки задачи.
		/// </summary>
		[Browsable(false)]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			private set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_extensionInfo.Clear();

				var syncCol = value as ISynchronizedCollection;

				if (syncCol == null)
				{
					_extensionInfo.AddRange(value);
				}
				else
				{
					lock (syncCol.SyncRoot)
					{
						_extensionInfo.AddRange(value);
					}
				}
			}
		}

		/// <summary>
		/// Инструменты, связанные с задачей.
		/// </summary>
		[RelationMany(typeof(HydraTaskSecurityList))]
		[Browsable(false)]
		public HydraTaskSecurityList Securities { get; protected set; }

		/// <summary>
		/// Применить изменения, сделанные в копии настроек.
		/// </summary>
		/// <param name="settingsCopy">Копия.</param>
		public virtual void ApplyChanges(HydraTaskSettings settingsCopy)
		{
			Id = settingsCopy.Id;
			TaskType = settingsCopy.TaskType;
			ExtensionInfo.Clear();
			ExtensionInfo.AddRange(settingsCopy.ExtensionInfo);
			SupportedLevel1Fields = settingsCopy.SupportedLevel1Fields.ToArray();

			ExtensionInfo.Keys.OfType<string>().ForEach(NotifyPropertyChanged);
		}

		/// <summary>
		/// Создать копию <see cref="HydraTaskSettings"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public override HydraTaskSettings Clone()
		{
			var clone = GetType() == typeof(HydraTaskSettings)
				? new HydraTaskSettings()
				: GetType().CreateInstance<HydraTaskSettings>(this);

			clone.Id = Id;
			clone.TaskType = TaskType;
			clone.ExtensionInfo = ExtensionInfo;
			clone.Securities = Securities;
			clone.SupportedLevel1Fields = SupportedLevel1Fields.ToArray();

			return clone;
		}

		/// <summary>
		/// Вызвать событие <see cref="PropertyChanged"/>.
		/// </summary>
		/// <param name="name">Название свойства.</param>
		protected void NotifyPropertyChanged(string name)
		{
			PropertyChanged.SafeInvoke(this, name);
		}

		/// <summary>
		/// Событие изменения настроек.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}

	/// <summary>
	/// Настройки задачи <see cref="ConnectorHydraTask{TConnector}"/>.
	/// </summary>
	public abstract class ConnectorHydraTaskSettings : HydraTaskSettings
	{
		/// <summary>
		/// Инициализировать <see cref="ConnectorHydraTaskSettings"/>.
		/// </summary>
		/// <param name="settings">Реальные настройки.</param>
		protected ConnectorHydraTaskSettings(HydraTaskSettings settings)
			: base(settings)
		{
		}

		/// <summary>
		/// Поддерживаемые поля маркет-данных первого уровня.
		/// </summary>
		[Browsable(true)]
		public override IEnumerable<Level1Fields> SupportedLevel1Fields
		{
			get { return base.SupportedLevel1Fields; }
			set { base.SupportedLevel1Fields = value; }
		}

		private ReConnectionSettings _reConnectionSettings;

		/// <summary>
		/// Настройки контроля подключения <see cref="IConnector"/> к торговой системе.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str174Key)]
		[DescriptionLoc(LocalizedStrings.Str2249Key)]
		[Ignore]
		public ReConnectionSettings ReConnectionSettings
		{
			get
			{
				if (_reConnectionSettings != null)
					return _reConnectionSettings;

				_reConnectionSettings = new ReConnectionSettings();
					
				var settings = (SettingsStorage)ExtensionInfo.TryGetValue("ReConnectionSettings");

				if (settings != null)
					_reConnectionSettings.Load(settings);
				else
					ExtensionInfo.Add("ReConnectionSettings", _reConnectionSettings.Save());

				return _reConnectionSettings;
			}
		}

		/// <summary>
		/// Скачивать новости.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.NewsKey)]
		[DescriptionLoc(LocalizedStrings.Str2251Key)]
		[Ignore]
		[PropertyOrder(10)]
		[Browsable(false)]
		public virtual bool IsDownloadNews
		{
			get
			{
				var str = ExtensionInfo.TryGetValue("IsDownloadNews");
				return str != null && str.To<bool>();
			}
			set
			{
				ExtensionInfo["IsDownloadNews"] = value;
				NotifyPropertyChanged("IsDownloadNews");
			}
		}

		/// <summary>
		/// Применить изменения, сделанные в копии настроек.
		/// </summary>
		/// <param name="settingsCopy">Копия.</param>
		public override void ApplyChanges(HydraTaskSettings settingsCopy)
		{
			var settings = ((ConnectorHydraTaskSettings)settingsCopy)._reConnectionSettings;

			if (settings != null)
				settingsCopy.ExtensionInfo["ReConnectionSettings"] = settings.Save();

			base.ApplyChanges(settingsCopy);
		}
	}
}