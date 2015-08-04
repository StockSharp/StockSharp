namespace StockSharp.OpenECry
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Режимы подключения к терминалу.
	/// Описание функциональности http://www.openecry.com/api/OECAPIRemoting.pdf
	/// </summary>
	public enum OpenECryRemoting
	{
		/// <summary>
		/// Отключен.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2558Key)]
		None,

		/// <summary>
		/// Если существует другое подключение с теми же Login/Password, оно может быть разорвано.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.MainKey)]
		Primary,

		/// <summary>
		/// Попытка активировать в режиме <see cref="Secondary"/>, в случае неудачи - режим <see cref="None"/>.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2560Key)]
		Secondary
	}

	[DisplayName("OpenECry")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "OpenECry")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class OpenECryMessageAdapter
	{
		private EndPoint _address = OpenECryAddresses.Api;

		/// <summary>
		/// Адрес API сервера OpenECry. По-умолчанию равен <see cref="OpenECryAddresses.Api"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.AddressKey)]
		[DescriptionLoc(LocalizedStrings.Str2562Key)]
		[PropertyOrder(0)]
		public EndPoint Address
		{
			get { return _address; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_address = value;
			}
		}

		/// <summary>
		/// Имя пользователя OpenECry.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.Str2563Key)]
		[PropertyOrder(1)]
		public string Login { get; set; }

		/// <summary>
		/// Пароль пользователя OpenECry.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.Str2564Key)]
		[PropertyOrder(2)]
		public SecureString Password { get; set; }

		/// <summary>
		/// Уникальный идентификатор программного обеспечения.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayName("UUID")]
		[DescriptionLoc(LocalizedStrings.Str2565Key)]
		[PropertyOrder(3)]
		public string Uuid { get; set; }

		/// <summary>
		/// Требуемый режим подключения к терминалу. По умолчанию <see cref="OpenECryRemoting.None"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayName("Remoting")]
		[DescriptionLoc(LocalizedStrings.Str2566Key)]
		[PropertyOrder(4)]
		public OpenECryRemoting Remoting { get; set; }

		/// <summary>
		/// Использовать "родной" механизм восстановления соединения.
		/// По умолчанию включено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str180Key)]
		[DescriptionLoc(LocalizedStrings.Str2567Key)]
		[PropertyOrder(5)]
		public bool UseNativeReconnect { get; set; }

		/// <summary>
		/// Использовать логирование OpenECry API.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str2568Key)]
		[PropertyOrder(6)]
		public bool EnableOECLogging { get; set; }

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromSeconds(1),
			TimeSpan.FromMinutes(1),
			TimeSpan.FromHours(1),
			TimeSpan.FromDays(1),
			TimeSpan.FromTicks(TimeHelper.TicksPerWeek),
			TimeSpan.FromTicks(TimeHelper.TicksPerMonth)
		});

		/// <summary>
		/// Доступные тайм-фреймы.
		/// </summary>
		[Browsable(false)]
		public static IEnumerable<TimeSpan> TimeFrames
		{
			get { return _timeFrames; }
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Uuid", Uuid);
			storage.SetValue("Address", Address.To<string>());
			storage.SetValue("Login", Login);
			storage.SetValue("Password", Password);
			storage.SetValue("Remoting", Remoting.To<string>());
			storage.SetValue("UseNativeReconnect", UseNativeReconnect);
			storage.SetValue("EnableOECLogging", EnableOECLogging);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Uuid = storage.GetValue<string>("Uuid");
			Address = storage.GetValue<EndPoint>("Address");
			Login = storage.GetValue<string>("Login");
			Password = storage.GetValue<SecureString>("Password");
			Remoting = storage.GetValue<OpenECryRemoting>("Remoting");
			UseNativeReconnect = storage.GetValue<bool>("UseNativeReconnect");
			EnableOECLogging = storage.GetValue<bool>("EnableOECLogging");
		}
	}
}