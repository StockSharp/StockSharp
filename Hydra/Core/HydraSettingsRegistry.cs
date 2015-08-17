namespace StockSharp.Hydra.Core
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.History.Hydra;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Класс для представления всех настроек.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2211Key)]
	[CategoryOrderLoc(LocalizedStrings.GeneralKey, 0)]
	[CategoryOrder(_serverCategory, 1)]
	[CategoryOrder("CSV", 2)]
	public class HydraSettingsRegistry : IPersistable
	{
		private const string _serverCategory = "S#.Data Server";

		/// <summary>
		/// Создать <see cref="HydraSettingsRegistry"/>.
		/// </summary>
		public HydraSettingsRegistry()
		{
		}

		/// <summary>
		/// Серверный режим.
		/// </summary>
		[Category(_serverCategory)]
		[DisplayNameLoc(LocalizedStrings.Str2212Key)]
		[DescriptionLoc(LocalizedStrings.Str2213Key)]
		[PropertyOrder(0)]
		public bool IsServer { get; set; }

		private int _maxSecurityCount = RemoteStorage.DefaultMaxSecurityCount;

		/// <summary>
		/// Максимальное количество инструментов, которое можно запрашивать с сервера.
		/// </summary>
		[Category(_serverCategory)]
		[DisplayNameLoc(LocalizedStrings.Str2214Key)]
		[DescriptionLoc(LocalizedStrings.Str2215Key)]
		[PropertyOrder(1)]
		public int MaxSecurityCount
		{
			get { return _maxSecurityCount; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_maxSecurityCount = value;
			}
		}

		/// <summary>
		/// Авторизация для получения доступа к S#.Data сервер.
		/// </summary>
		[Category(_serverCategory)]
		[DisplayNameLoc(LocalizedStrings.AuthorizationKey)]
		[DescriptionLoc(LocalizedStrings.Str2216Key)]
		[PropertyOrder(1)]
		public AuthorizationModes Authorization { get; set; }

		/// <summary>
		/// Автостарт скачивания котировок при запуске.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2217Key)]
		[DescriptionLoc(LocalizedStrings.Str2218Key)]
		[PropertyOrder(0)]
		public bool AutoStart { get; set; }

		/// <summary>
		/// Сворачивать в трей.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2219Key)]
		[DescriptionLoc(LocalizedStrings.Str2220Key)]
		[PropertyOrder(1)]
		public bool MinimizeToTray { get; set; }

		/// <summary>
		/// Остановка работы по времени.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2221Key)]
		[DescriptionLoc(LocalizedStrings.Str2222Key)]
		[PropertyOrder(2)]
		public bool AutoStop { get; set; }

		private TimeSpan _stopTime;

		/// <summary>
		/// Время для Авто-стоп.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2223Key)]
		[DescriptionLoc(LocalizedStrings.Str2223Key, true)]
		[PropertyOrder(3)]
		public TimeSpan StopTime
		{
			get { return _stopTime; }
			set
			{
				if (value < TimeSpan.Zero || value.TotalHours > 24)
					throw new ArgumentOutOfRangeException();

				_stopTime = value;
			}
		}

		private int _emailErrorCount;

		/// <summary>
		/// Количество ошибок, после которых будет отправлено письмо на почту с сообщение об ошибке.
		/// Значение 0 означает выключенный режим.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2225Key)]
		[DescriptionLoc(LocalizedStrings.Str2226Key)]
		[PropertyOrder(4)]
		public int EmailErrorCount
		{
			get { return _emailErrorCount; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_emailErrorCount = value;
			}
		}

		/// <summary>
		/// Адрес почты, куда будет отправлено письмо с сообщением о превышении максимально допустимого количества ошибок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2227Key)]
		[DescriptionLoc(LocalizedStrings.Str2228Key)]
		[PropertyOrder(5)]
		public string EmailErrorAddress { get; set; }

		private TemplateTxtRegistry _templateTxtRegistry = new TemplateTxtRegistry();

		/// <summary>
		/// Реестр шаблонов для экспорта в формат txt.
		/// </summary>
		[Category("CSV")]
		[DisplayName(LocalizedStrings.TemplateKey)]
		[DescriptionLoc(LocalizedStrings.TemplateKey, true)]
		[ExpandableObject]
		public TemplateTxtRegistry TemplateTxtRegistry
		{
			get { return _templateTxtRegistry; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_templateTxtRegistry = value;
			}
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			AutoStart = storage.GetValue<bool>("AutoStart");
			MinimizeToTray = storage.GetValue("MinimizeToTray", false);
			IsServer = storage.GetValue<bool>("IsServer");
			MaxSecurityCount = storage.GetValue("MaxSecurityCount", MaxSecurityCount);
			Authorization = storage.GetValue<AuthorizationModes>("Authorization");
			AutoStop = storage.GetValue<bool>("AutoStop");
			StopTime = storage.GetValue("StopTime", 0L).To<TimeSpan>();
			EmailErrorCount = storage.GetValue<int>("EmailErrorCount");
			EmailErrorAddress = storage.GetValue<string>("EmailErrorAddress");
			
			TemplateTxtRegistry.Load(storage);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("AutoStart", AutoStart);
			storage.SetValue("MinimizeToTray", MinimizeToTray);
			storage.SetValue("IsServer", IsServer);
			storage.SetValue("MaxSecurityCount", MaxSecurityCount);
			storage.SetValue("Authorization", Authorization.To<string>());
			storage.SetValue("AutoStop", AutoStop);
			storage.SetValue("StopTime", StopTime.To<long>());
			storage.SetValue("EmailErrorCount", EmailErrorCount);
			storage.SetValue("EmailErrorAddress", EmailErrorAddress);
			
			TemplateTxtRegistry.Save(storage);
		}
	}
}