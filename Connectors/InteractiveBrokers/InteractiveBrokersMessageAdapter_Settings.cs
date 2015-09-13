namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayName("Interactive Brokers")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "Interactive Brokers")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class InteractiveBrokersMessageAdapter
	{
		/// <summary>
		/// Адрес по-умолчанию.
		/// </summary>
		public static readonly EndPoint DefaultAddress = new IPEndPoint(IPAddress.Loopback, 7496);

		/// <summary>
		/// Адрес по-умолчанию.
		/// </summary>
		public static readonly EndPoint DefaultGatewayAddress = new IPEndPoint(IPAddress.Loopback, 4001);

		/// <summary>
		/// Адрес.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.AddressKey)]
		[DescriptionLoc(LocalizedStrings.AddressKey)]
		[PropertyOrder(1)]
		public EndPoint Address { get; set; }

		/// <summary>
		/// Уникальный идентификатор. Используется в случае подключения нескольких клиентов к одному терминалу или gateway.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.Str2518Key)]
		[PropertyOrder(2)]
		public int ClientId { get; set; }

		/// <summary>
		/// Использовать ли данные реального времени или "замороженные" на сервере брокера. По-умолчанию используются "замороженные" данные.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.RealTimeKey)]
		[DescriptionLoc(LocalizedStrings.Str2520Key)]
		[PropertyOrder(3)]
		public bool IsRealTimeMarketData { get; set; }

		/// <summary>
		/// Уровень логирования сообщений сервера. По-умолчанию равен <see cref="ServerLogLevels.Detail"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str2521Key)]
		[PropertyOrder(4)]
		public ServerLogLevels ServerLogLevel { get; set; }

		private IEnumerable<GenericFieldTypes> _fields = Enumerable.Empty<GenericFieldTypes>();

		/// <summary>
		/// Поля маркет-данных, которые будут получаться при подписке на Level1 сообщения.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str2522Key)]
		[DescriptionLoc(LocalizedStrings.Str2523Key)]
		[PropertyOrder(4)]
		public IEnumerable<GenericFieldTypes> Fields
		{
			get { return _fields; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_fields = value;
			}
		}

		/// <summary>
		/// Время подключения.
		/// </summary>
		[Browsable(false)]
		public DateTime ConnectedTime { get; internal set; }

		/// <summary>
		/// Экстра подключение.
		/// </summary>
		[Browsable(false)]
		public bool ExtraAuth { get; set; }

		/// <summary>
		/// Дополнительные возможности.
		/// </summary>
		[Browsable(false)]
		public string OptionalCapabilities { get; set; }

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Address = storage.GetValue<EndPoint>("Address");
			ClientId = storage.GetValue<int>("ClientId");
			IsRealTimeMarketData = storage.GetValue<bool>("IsRealTimeMarketData");
			ServerLogLevel = storage.GetValue<ServerLogLevels>("ServerLogLevel");
			Fields = storage.GetValue<string>("Fields").Split(",").Select(n => n.To<GenericFieldTypes>()).ToArray();
			ExtraAuth = storage.GetValue<bool>("ExtraAuth");
			OptionalCapabilities = storage.GetValue<string>("OptionalCapabilities");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Address", Address.To<string>());
			storage.SetValue("ClientId", ClientId);
			storage.SetValue("IsRealTimeMarketData", IsRealTimeMarketData);
			storage.SetValue("ServerLogLevel", ServerLogLevel.To<string>());
			storage.SetValue("Fields", Fields.Select(t => t.To<string>()).Join(","));
			storage.SetValue("ExtraAuth", ExtraAuth);
			storage.SetValue("OptionalCapabilities", OptionalCapabilities);
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str2526Params.Put(Address);
		}
	}
}