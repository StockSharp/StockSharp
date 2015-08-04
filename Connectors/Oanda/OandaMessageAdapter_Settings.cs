namespace StockSharp.Oanda
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Виды серверов.
	/// </summary>
	public enum OandaServers
	{
		/// <summary>
		/// Демо.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DemoKey)]
		Sandbox,

		/// <summary>
		/// Симулятор.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1209Key)]
		Practice,

		/// <summary>
		/// Реальный.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3448Key)]
		Real,
	}

	[DisplayName("OANDA")]
	[CategoryLoc(LocalizedStrings.ForexKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "OANDA")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class OandaMessageAdapter
	{
		/// <summary>
		/// Сервер.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3416Key)]
		[DescriptionLoc(LocalizedStrings.Str3450Key)]
		[PropertyOrder(0)]
		public OandaServers Server { get; set; }

		/// <summary>
		/// Токен.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3451Key)]
		[DescriptionLoc(LocalizedStrings.Str3451Key, true)]
		[PropertyOrder(1)]
		public SecureString Token { get; set; }

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromSeconds(5),
			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(15),
			TimeSpan.FromSeconds(30),
			TimeSpan.FromMinutes(1),
			TimeSpan.FromMinutes(2),
			TimeSpan.FromMinutes(3),
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(10),
			TimeSpan.FromMinutes(15),
			TimeSpan.FromMinutes(30),
			TimeSpan.FromHours(1),
			TimeSpan.FromHours(2),
			TimeSpan.FromHours(3),
			TimeSpan.FromHours(4),
			TimeSpan.FromHours(6),
			TimeSpan.FromHours(8),
			TimeSpan.FromHours(12),
			TimeSpan.FromDays(1),
			TimeSpan.FromDays(7),
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

			storage.SetValue("Server", Server.To<string>());
			storage.SetValue("Token", Token);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Server = storage.GetValue<OandaServers>("Server");
			Token = storage.GetValue<SecureString>("Token");
		}
	}
}