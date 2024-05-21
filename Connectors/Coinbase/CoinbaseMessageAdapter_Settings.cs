namespace StockSharp.Coinbase
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter for <see cref="Coinbase"/>.
	/// </summary>
	[MediaIcon("Coinbase_logo.svg")]
	[Doc("topics/api/connectors/crypto_exchanges/coinbase.html")]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CoinbaseKey,
		Description = LocalizedStrings.CryptoConnectorKey,
		GroupName = LocalizedStrings.CryptocurrencyKey)]
	[MessageAdapterCategory(MessageAdapterCategories.Crypto | MessageAdapterCategories.RealTime | MessageAdapterCategories.OrderLog |
		MessageAdapterCategories.Free | MessageAdapterCategories.Level1 | MessageAdapterCategories.Transactions)]
	public partial class CoinbaseMessageAdapter : IKeySecretAdapter, IPassphraseAdapter
	{
		private static readonly HashSet<TimeSpan> _timeFrames = new(new[]
		{
			TimeSpan.FromMinutes(1),
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(15),
			TimeSpan.FromHours(1),
			TimeSpan.FromHours(6),
			TimeSpan.FromDays(1),
		});

		/// <summary>
		/// Possible time-frames.
		/// </summary>
		public static IEnumerable<TimeSpan> AllTimeFrames => _timeFrames;

		/// <inheritdoc />
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.KeyKey,
			Description = LocalizedStrings.KeyKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.ConnectionKey,
			Order = 0)]
		public SecureString Key { get; set; }

		/// <inheritdoc />
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SecretKey,
			Description = LocalizedStrings.SecretDescKey,
			GroupName = LocalizedStrings.ConnectionKey,
			Order = 1)]
		public SecureString Secret { get; set; }

		/// <summary>
		/// Passphrase.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PassphraseKey,
			Description = LocalizedStrings.PassphraseKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.ConnectionKey,
			Order = 2)]
		public SecureString Passphrase { get; set; }

		private TimeSpan _balanceCheckInterval;

		/// <summary>
		/// Balance check interval. Required in case of deposit and withdraw actions.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BalanceKey,
			Description = LocalizedStrings.BalanceCheckIntervalKey,
			GroupName = LocalizedStrings.ConnectionKey,
			Order = 3)]
		public TimeSpan BalanceCheckInterval
		{
			get => _balanceCheckInterval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value));

				_balanceCheckInterval = value;
			}
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Key), Key);
			storage.SetValue(nameof(Secret), Secret);
			storage.SetValue(nameof(Passphrase), Passphrase);
			storage.SetValue(nameof(BalanceCheckInterval), BalanceCheckInterval);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Key = storage.GetValue<SecureString>(nameof(Key));
			Secret = storage.GetValue<SecureString>(nameof(Secret));
			Passphrase = storage.GetValue<SecureString>(nameof(Passphrase));
			BalanceCheckInterval = storage.GetValue<TimeSpan>(nameof(BalanceCheckInterval));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + ": " + LocalizedStrings.Key + " = " + Key.ToId();
		}
	}
}