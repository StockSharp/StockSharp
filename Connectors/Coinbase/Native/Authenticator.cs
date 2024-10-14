using System;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using StockSharp.Coinbase.Native;
using HttpClient = System.Net.Http.HttpClient;

class Authenticator : Disposable
{
    private readonly HashAlgorithm _hasher;

    public Authenticator(SecureString name, SecureString token)
    {
        Name = name;
        Token = token;

        // Erzeugung des HMACSHA256-Hashers mit dem Token (als Byte-Array)
        _hasher = token.IsEmpty() ? null : new HMACSHA256(Encoding.UTF8.GetBytes(ToUnsecureString(token)));
    }

    protected override void DisposeManaged()
    {
        _hasher?.Dispose();
        base.DisposeManaged();
    }

    public SecureString Name { get; }
    public SecureString Token { get; }

    public string MakeSign(string url, Method method, string parameters, out string timestamp)
    {
        timestamp = DateTime.UtcNow.ToUnix().ToString("F0");

        string message = timestamp + method.ToString().ToUpperInvariant() + url + parameters;
        byte[] data = Encoding.UTF8.GetBytes(message);

        if (_hasher != null)
        {
            byte[] signatureBytes = _hasher.ComputeHash(data);
            return Convert.ToBase64String(signatureBytes);
        }
        else
        {
            throw new InvalidOperationException("Token is empty. Cannot generate signature.");
        }
    }

  

    private string ToUnsecureString(SecureString secureString)
    {
        if (secureString == null)
            throw new ArgumentNullException(nameof(secureString));

        IntPtr bstr = IntPtr.Zero;
        try
        {
            bstr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(secureString);
            return System.Runtime.InteropServices.Marshal.PtrToStringBSTR(bstr);
        }
        finally
        {
            if (bstr != IntPtr.Zero)
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(bstr);
        }
    }
}
