namespace StockSharp.Community
{
	using System;
	using System.Security;

	using Ecng.ComponentModel;

	using StockSharp.Community.Messages;

	/// <summary>
	/// The interface describing a client for access to the StockSharp authentication service.
	/// </summary>
	public interface IAuthenticationClient : IDisposable
	{
		/// <summary>
		/// Information about the login and password for access to the StockSharp.
		/// </summary>
		ServerCredentials Credentials { get; }

		/// <summary>
		/// Product.
		/// </summary>
		ProductInfoMessage Product { get; set; }

		/// <summary>
		/// Version.
		/// </summary>
		Version Version { get; set; }

		/// <summary>
		/// Has the client successfully authenticated.
		/// </summary>
		bool IsLoggedIn { get; }

		/// <summary>
		/// Session ID.
		/// </summary>
		Guid SessionId { get; }

		/// <summary>
		/// The user identifier for <see cref="SessionId"/>.
		/// </summary>
		long UserId { get; }

		/// <summary>
		/// To log in.
		/// </summary>
		/// <returns>Session ID.</returns>
		Tuple<Guid, long> Login();

		/// <summary>
		/// To log in.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="version">Version.</param>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <returns>Session ID.</returns>
		Tuple<Guid, long> Login(ProductInfoMessage product, Version version, string login, SecureString password);

		/// <summary>
		/// To log in.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="version">Version.</param>
		/// <param name="token">Token.</param>
		/// <returns>Session ID.</returns>
		Tuple<Guid, long> Login(ProductInfoMessage product, Version version, SecureString token);

		/// <summary>
		/// Logout.
		/// </summary>
		void Logout();
	}
}