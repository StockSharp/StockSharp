namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
	using StockSharp.Logging;

	/// <summary>
	/// Корневой объект для доступа к базе данных Hydra.
	/// </summary>
	public class HydraEntityRegistry : EntityRegistry
	{
		private sealed class HydraSettingsList : BaseStorageEntityList<HydraSettings>
		{
			public HydraSettingsList(IStorage storage)
				: base(storage)
			{
			}
		}

		private readonly HydraSettingsList _settingsList;

		/// <summary>
		/// Создать <see cref="HydraEntityRegistry"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public HydraEntityRegistry(IStorage storage)
			: base(storage)
		{
			SerializationContext.DelayAction = DelayAction = new DelayAction(storage, ex => ex.LogError());

			TasksSettings = new HydraTaskSettingsList(storage) { DelayAction = DelayAction };
			_settingsList = new HydraSettingsList(storage) { DelayAction = DelayAction };
		}

		/// <summary>
		/// Настройки задач <see cref="IHydraTask"/>.
		/// </summary>
		public HydraTaskSettingsList TasksSettings { get; private set; }

		/// <summary>
		/// Последняя версия, которой должна быть актуальная база данных Hydra.
		/// </summary>
		public static readonly Version LatestVersion = new Version(2, 13);

		/// <summary>
		/// Версия.
		/// </summary>
		public Version Version
		{
			get { return GetValue("Version", new Version(1, 0, 0, 1)); }
			set { SetValue("Version", value); }
		}

		private HydraSettingsRegistry _settings;

		/// <summary>
		/// Настройки.
		/// </summary>
		public HydraSettingsRegistry Settings
		{
			get
			{
				if (_settings != null) 
					return _settings;

				_settings = new HydraSettingsRegistry();

				var storage = new SettingsStorage();
				storage.AddRange(SettingsDict.Select(p => new KeyValuePair<string, object>(p.Key, p.Value.Value)));
				_settings.Load(storage);

				return _settings;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_settings = value;

				var storage = new SettingsStorage();

				_settings.Save(storage);

				foreach (var pair in storage)
					SetValue(pair.Key, pair.Value);
			}
		}

		private Dictionary<string, HydraSettings> _settingsDict;

		private Dictionary<string, HydraSettings> SettingsDict
		{
			get { return _settingsDict ?? (_settingsDict = _settingsList.ToDictionary(s => s.Name, s => s)); }
		}

		private T GetValue<T>(string name, T defaultValue = default(T))
		{
			return SettingsDict.SafeAdd(name, key => new HydraSettings { Name = name, Value = defaultValue.To<string>() }).Value.To<T>();
		}

		private void SetValue<T>(string name, T value)
		{
			var settings = SettingsDict.TryGetValue(name);

			if (settings == null)
			{
				settings = new HydraSettings { Name = name };
				SettingsDict.Add(name, settings);
			}

			settings.Value = value.To<string>();

			_settingsList.Save(settings);
		}
	}
}