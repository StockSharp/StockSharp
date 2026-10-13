namespace StockSharp.Configuration;

/// <summary>
/// Fetches the credentials of the currently signed-in client.
/// </summary>
/// <remarks>
/// The credentials dialog needs exactly one thing from the web API - who is signed in and with
/// what token - and this is that one thing. Stating it here, next to ICredentialsProvider, rather
/// than taking the web API's own client service keeps the dependency pointing the right way: the
/// dialog does not need to know the shape of the web API, and the caller, which already knows it,
/// supplies the implementation.
/// </remarks>
public interface ICurrentClientFetcher
{
	/// <summary>
	/// Returns the signed-in client's email and access token.
	/// </summary>
	/// <param name="salt">
	/// OAuth salt to wait on, or <see langword="null"/> for a plain "who am I" request.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The client's email and access token.</returns>
	Task<(string email, SecureString accessToken)> FetchAsync(string salt, CancellationToken cancellationToken);
}
