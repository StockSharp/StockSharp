namespace StockSharp.Bittrex.Native;

using System.Security.Cryptography;

class Authenticator : Disposable
{
	private readonly HashAlgorithm _hasher;

	public Authenticator(SecureString key, SecureString secret)
	{
		Key = key;
		_hasher = secret.IsEmpty() ? null : new HMACSHA512(secret.UnSecure().UTF8());
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	public SecureString Key { get; }

	public string MakeSign(string data)
	{
		return _hasher
		       .ComputeHash(data.UTF8())
		       .Digest()
		       .ToLowerInvariant();
	}
}