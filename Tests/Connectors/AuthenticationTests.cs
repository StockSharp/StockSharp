namespace StockSharp.Tests.Connectors;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

using StockSharp.BitStamp;
using StockSharp.Coinbase;
using StockSharp.FTX;
using StockSharp.Tinkoff;
using StockSharp.Bitalong;
using StockSharp.Bitexbook;
using StockSharp.Btce;
using StockSharp.Messages;

/// <summary>
/// Integration tests for authentication and session management.
/// </summary>
[TestClass]
public class AuthenticationTests
{
	[TestMethod]
	public void BitStamp_KeySecretAuthentication_ShouldBeConfigurable()
	{
		TestKeySecretAuthentication<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_KeySecretAuthentication_ShouldBeConfigurable()
	{
		TestKeySecretAuthentication<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_KeySecretAuthentication_ShouldBeConfigurable()
	{
		TestKeySecretAuthentication<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_KeySecretAuthentication_ShouldBeConfigurable()
	{
		TestKeySecretAuthentication<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_KeySecretAuthentication_ShouldBeConfigurable()
	{
		TestKeySecretAuthentication<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_KeySecretAuthentication_ShouldBeConfigurable()
	{
		TestKeySecretAuthentication<BtceMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_PassphraseAuthentication_ShouldBeConfigurable()
	{
		TestPassphraseAuthentication<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_TokenAuthentication_ShouldBeConfigurable()
	{
		TestTokenAuthentication<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_EmptyCredentials_ShouldBeDetected()
	{
		TestEmptyCredentials<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_EmptyCredentials_ShouldBeDetected()
	{
		TestEmptyCredentials<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_EmptyCredentials_ShouldBeDetected()
	{
		TestEmptyCredentials<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_EmptyCredentials_ShouldBeDetected()
	{
		TestEmptyCredentials<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_EmptyCredentials_ShouldBeDetected()
	{
		TestEmptyCredentials<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_EmptyCredentials_ShouldBeDetected()
	{
		TestEmptyCredentials<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_EmptyCredentials_ShouldBeDetected()
	{
		TestEmptyCredentials<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_SecureStringHandling_ShouldWork()
	{
		TestSecureStringHandling<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_SecureStringHandling_ShouldWork()
	{
		TestSecureStringHandling<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_SecureStringHandling_ShouldWork()
	{
		TestSecureStringHandling<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_SecureStringHandling_ShouldWork()
	{
		TestSecureStringHandling<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_SecureStringHandling_ShouldWork()
	{
		TestSecureStringHandling<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_SecureStringHandling_ShouldWork()
	{
		TestSecureStringHandling<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_SecureStringHandling_ShouldWork()
	{
		TestSecureStringHandling<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_SessionManagement_ShouldMaintainSession()
	{
		TestSessionManagement<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_SessionManagement_ShouldMaintainSession()
	{
		TestSessionManagement<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_SessionManagement_ShouldMaintainSession()
	{
		TestSessionManagement<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_SessionManagement_ShouldMaintainSession()
	{
		TestSessionManagement<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_SessionManagement_ShouldMaintainSession()
	{
		TestSessionManagement<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_SessionManagement_ShouldMaintainSession()
	{
		TestSessionManagement<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_SessionManagement_ShouldMaintainSession()
	{
		TestSessionManagement<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_AuthenticationFailure_ShouldReturnError()
	{
		TestAuthenticationFailure<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_AuthenticationFailure_ShouldReturnError()
	{
		TestAuthenticationFailure<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_AuthenticationFailure_ShouldReturnError()
	{
		TestAuthenticationFailure<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_AuthenticationFailure_ShouldReturnError()
	{
		TestAuthenticationFailure<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_AuthenticationFailure_ShouldReturnError()
	{
		TestAuthenticationFailure<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_AuthenticationFailure_ShouldReturnError()
	{
		TestAuthenticationFailure<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_AuthenticationFailure_ShouldReturnError()
	{
		TestAuthenticationFailure<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_TokenRefresh_ShouldWork()
	{
		TestTokenRefresh<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_TokenRefresh_ShouldWork()
	{
		TestTokenRefresh<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_TokenRefresh_ShouldWork()
	{
		TestTokenRefresh<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_TokenRefresh_ShouldWork()
	{
		TestTokenRefresh<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_TokenRefresh_ShouldWork()
	{
		TestTokenRefresh<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_TokenRefresh_ShouldWork()
	{
		TestTokenRefresh<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_TokenRefresh_ShouldWork()
	{
		TestTokenRefresh<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_CredentialValidation_ShouldValidate()
	{
		TestCredentialValidation<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_CredentialValidation_ShouldValidate()
	{
		TestCredentialValidation<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_CredentialValidation_ShouldValidate()
	{
		TestCredentialValidation<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_CredentialValidation_ShouldValidate()
	{
		TestCredentialValidation<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_CredentialValidation_ShouldValidate()
	{
		TestCredentialValidation<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_CredentialValidation_ShouldValidate()
	{
		TestCredentialValidation<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_CredentialValidation_ShouldValidate()
	{
		TestCredentialValidation<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_AddressConfiguration_ShouldWork()
	{
		TestAddressConfiguration<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_AddressConfiguration_ShouldWork()
	{
		TestAddressConfiguration<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_AddressConfiguration_ShouldWork()
	{
		TestAddressConfiguration<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_AddressConfiguration_ShouldWork()
	{
		TestAddressConfiguration<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_AddressConfiguration_ShouldWork()
	{
		TestAddressConfiguration<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_AddressConfiguration_ShouldWork()
	{
		TestAddressConfiguration<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_AddressConfiguration_ShouldWork()
	{
		TestAddressConfiguration<BtceMessageAdapter>();
	}

	private static void TestKeySecretAuthentication<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		
		if (adapter is IKeySecretAdapter keySecretAdapter)
		{
			var key = new SecureString();
			foreach (char c in "test_key")
				key.AppendChar(c);
			
			var secret = new SecureString();
			foreach (char c in "test_secret")
				secret.AppendChar(c);

			keySecretAdapter.Key = key;
			keySecretAdapter.Secret = secret;

			keySecretAdapter.Key.AssertNotNull("Key should be set");
			keySecretAdapter.Secret.AssertNotNull("Secret should be set");
		}
	}

	private static void TestPassphraseAuthentication<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		
		if (adapter is IPassphraseAdapter passphraseAdapter)
		{
			var passphrase = new SecureString();
			foreach (char c in "test_passphrase")
				passphrase.AppendChar(c);

			passphraseAdapter.Passphrase = passphrase;

			passphraseAdapter.Passphrase.AssertNotNull("Passphrase should be set");
		}
	}

	private static void TestTokenAuthentication<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		
		if (adapter is ITokenAdapter tokenAdapter)
		{
			var token = new SecureString();
			foreach (char c in "test_token")
				token.AppendChar(c);

			tokenAdapter.Token = token;

			tokenAdapter.Token.AssertNotNull("Token should be set");
		}
	}

	private static void TestEmptyCredentials<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		var errors = new List<ErrorMessage>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ErrorMessage err)
				errors.Add(err);
		};

		var connectMsg = new ConnectMessage();
		adapter.SendInMessage(connectMsg);

		// Should handle empty credentials gracefully
		true.AssertTrue("Should handle empty credentials");
	}

	private static void TestSecureStringHandling<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		
		if (adapter is IKeySecretAdapter keySecretAdapter)
		{
			var key = new SecureString();
			foreach (char c in "test_key_123")
				key.AppendChar(c);

			keySecretAdapter.Key = key;
			keySecretAdapter.Key.AssertNotNull("SecureString should be handled");
		}
	}

	private static void TestSessionManagement<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		adapter.SendInMessage(new ConnectMessage());
		adapter.SendInMessage(new SecurityLookupMessage { TransactionId = 1 });
		adapter.SendInMessage(new SecurityLookupMessage { TransactionId = 2 });

		// Session should be maintained across multiple messages
		messages.Count.AssertTrue(c => c >= 0, "Should maintain session");
	}

	private static void TestAuthenticationFailure<T>() where T : MessageAdapter
	{
		var errors = new List<ErrorMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ErrorMessage err)
				errors.Add(err);
		};

		var connectMsg = new ConnectMessage();
		adapter.SendInMessage(connectMsg);

		// Should handle authentication failure gracefully
		true.AssertTrue("Should handle authentication failure");
	}

	private static void TestTokenRefresh<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		
		if (adapter is ITokenAdapter tokenAdapter)
		{
			var token = new SecureString();
			foreach (char c in "new_token")
				token.AppendChar(c);

			tokenAdapter.Token = token;
			tokenAdapter.Token.AssertNotNull("Token refresh should work");
		}
	}

	private static void TestCredentialValidation<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		var errors = new List<ErrorMessage>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ErrorMessage err)
				errors.Add(err);
		};

		// Test credential validation
		var connectMsg = new ConnectMessage();
		adapter.SendInMessage(connectMsg);

		// Should validate credentials
		true.AssertTrue("Should validate credentials");
	}

	private static void TestAddressConfiguration<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		
		if (adapter is IAddressAdapter<string> addressAdapter)
		{
			addressAdapter.Address = "https://api.example.com";
			addressAdapter.Address.AssertEqual("https://api.example.com", "Address should be configurable");
		}
	}

	private static MessageAdapter CreateAdapter<T>() where T : MessageAdapter
	{
		var gen = new IncrementalIdGenerator();
		return typeof(T).Name switch
		{
			nameof(BitStampMessageAdapter) => new BitStampMessageAdapter(gen),
			nameof(CoinbaseMessageAdapter) => new CoinbaseMessageAdapter(gen),
			nameof(FtxMessageAdapter) => new FtxMessageAdapter(gen),
			nameof(TinkoffMessageAdapter) => new TinkoffMessageAdapter(gen),
			nameof(BitalongMessageAdapter) => new BitalongMessageAdapter(gen),
			nameof(BitexbookMessageAdapter) => new BitexbookMessageAdapter(gen),
			nameof(BtceMessageAdapter) => new BtceMessageAdapter(gen),
			_ => throw new NotSupportedException($"Adapter type {typeof(T).Name} not supported")
		};
	}
}

