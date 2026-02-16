namespace StockSharp.PrizmBit.Native;

using System.Security.Cryptography;

class Authenticator(bool canSign, SecureString key, SecureString secret) : Disposable
{
	private readonly HashAlgorithm _hasher = secret.IsEmpty() ? null : new HMACSHA256(secret.UnSecure().UTF8());

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	public bool CanSign { get; } = canSign;

	public SecureString Key { get; } = key;
	public SecureString Secret { get; } = secret;

	public string SignatureType => "HMAC-SHA384";

	public string MakeSign(string parameters)
	{
		var signature = _hasher.ComputeHash(parameters.UTF8());

		return signature.Digest().ToUpperInvariant();
	}
}