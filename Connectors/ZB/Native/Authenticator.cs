namespace StockSharp.ZB.Native;

using System.Security.Cryptography;

class Authenticator : Disposable
{
	private readonly HashAlgorithm _hasher;

	public Authenticator(bool canSign, SecureString key, SecureString secret)
	{
		Key = key;
		CanSign = canSign;
		_hasher = CanSign ? new HMACMD5(secret.UnSecure().UTF8()) : null;
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	public SecureString Key { get; }
	public bool CanSign { get; }

	public string MakeSign(string input)
	{
		return _hasher
		       .ComputeHash(input.UTF8())
		       .Digest()
		       .ToLowerInvariant();
	}
}