namespace StockSharp.Coinbase.Native;

using System.Security;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

/// <summary>
/// Coinbase API authenticator. Supports both Legacy (HMAC) and CDP (JWT) authentication.
/// </summary>
class Authenticator : Disposable
{
	private readonly HashAlgorithm _hmac;
	private readonly ECDsa _ecdsa;

	/// <summary>
	/// Create authenticator.
	/// </summary>
	/// <param name="canSign">Can sign requests.</param>
	/// <param name="key">API key (Legacy) or CDP API key name.</param>
	/// <param name="secret">Secret key (Legacy base64) or CDP private key (EC PEM format).</param>
	/// <param name="passphrase">Passphrase (Legacy only, null for CDP).</param>
	public Authenticator(bool canSign, SecureString key, SecureString secret, SecureString passphrase)
	{
		CanSign = canSign;
		Key = key;
		Secret = secret;
		Passphrase = passphrase;

		if (secret.IsEmpty())
			return;

		var secretStr = secret.UnSecure();

		// Detect auth type: PEM format starts with "-----BEGIN"
		if (secretStr.Contains("-----BEGIN"))
		{
			// CDP authentication (JWT/ES256)
			UseLegacyAuth = false;
			_ecdsa = ECDsa.Create();
			_ecdsa.ImportFromPem(secretStr);
		}
		else
		{
			// Legacy authentication (HMAC-SHA256)
			UseLegacyAuth = true;
			_hmac = new HMACSHA256(secretStr.Base64());
		}
	}

	protected override void DisposeManaged()
	{
		_hmac?.Dispose();
		_ecdsa?.Dispose();
		base.DisposeManaged();
	}

	public bool CanSign { get; }
	public SecureString Key { get; }
	public SecureString Secret { get; }
	public SecureString Passphrase { get; }

	/// <summary>
	/// True if using Legacy HMAC auth, false if using CDP JWT auth.
	/// </summary>
	public bool UseLegacyAuth { get; }

	#region Legacy HMAC Authentication

	/// <summary>
	/// Make HMAC signature for Legacy authentication.
	/// </summary>
	public string MakeHmacSign(string url, Method method, string body, out string timestamp)
	{
		timestamp = DateTime.UtcNow.ToUnix().ToString("F0");

		return _hmac
			.ComputeHash((timestamp + method.ToString().ToUpperInvariant() + url + body).UTF8())
			.Base64();
	}

	#endregion

	#region CDP JWT Authentication

	/// <summary>
	/// Generate JWT token for REST API request (CDP auth).
	/// </summary>
	public string GenerateJwt(string method, string host, string path)
	{
		if (_ecdsa == null)
			return null;

		var keyName = Key.UnSecure();
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var nonce = GenerateNonce();

		var header = new
		{
			alg = "ES256",
			kid = keyName,
			nonce,
			typ = "JWT"
		};

		var payload = new
		{
			sub = keyName,
			iss = "cdp",
			nbf = now,
			exp = now + 120,
			uri = $"{method} {host}{path}"
		};

		return BuildJwt(header, payload);
	}

	/// <summary>
	/// Generate JWT token for WebSocket authentication (CDP auth).
	/// </summary>
	public string GenerateWebSocketJwt()
	{
		if (_ecdsa == null)
			return null;

		var keyName = Key.UnSecure();
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var nonce = GenerateNonce();

		var header = new
		{
			alg = "ES256",
			kid = keyName,
			nonce,
			typ = "JWT"
		};

		var payload = new
		{
			sub = keyName,
			iss = "cdp",
			nbf = now,
			exp = now + 120
		};

		return BuildJwt(header, payload);
	}

	private string BuildJwt(object header, object payload)
	{
		var headerJson = JsonConvert.SerializeObject(header);
		var payloadJson = JsonConvert.SerializeObject(payload);

		var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
		var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

		var dataToSign = $"{headerBase64}.{payloadBase64}";
		var signature = _ecdsa.SignData(Encoding.UTF8.GetBytes(dataToSign), HashAlgorithmName.SHA256);
		var signatureBase64 = Base64UrlEncode(signature);

		return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
	}

	private static string GenerateNonce()
	{
		var bytes = new byte[16];
		RandomNumberGenerator.Fill(bytes);
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}

	private static string Base64UrlEncode(byte[] data)
	{
		return data.Base64()
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
	}

	#endregion
}
