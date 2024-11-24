namespace StockSharp.Coinbase.Native;

using System.Security;
using System.Security.Cryptography;

class Authenticator : Disposable
{
	private readonly HashAlgorithm _hasher;

	public Authenticator(bool canSign, SecureString key, SecureString secret, SecureString passphrase)
	{
		CanSign = canSign;
		Key = key;
		Secret = secret;
		Passphrase = passphrase;

		_hasher = secret.IsEmpty() ? null : new HMACSHA256(secret.UnSecure().Base64());
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	public bool CanSign { get; }
	public SecureString Key { get; }
	public SecureString Secret { get; }
	public SecureString Passphrase { get; }

	public string MakeSign(string url, Method method, string parameters, out string timestamp)
	{
		timestamp = DateTime.UtcNow.ToUnix().ToString("F0");

		return _hasher
			.ComputeHash((timestamp + method.ToString().ToUpperInvariant() + url + parameters).UTF8())
			.Base64();
	}
}
