namespace StockSharp.Community
{
	using System;
	using System.Security;

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
		Products? Product { get; set; }

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
		/// To get the <see cref="SessionId"/> if the user was authorized.
		/// </summary>
		Guid? NullableSessionId { get; }

		/// <summary>
		/// The user identifier for <see cref="SessionId"/>.
		/// </summary>
		long UserId { get; }

		/// <summary>
		/// To log in.
		/// </summary>
		void Login();

		/// <summary>
		/// To log in.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="version">Version.</param>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		void Login(Products? product, Version version, string login, SecureString password);

		/// <summary>
		/// Logout.
		/// </summary>
		void Logout();

		/// <summary>
		/// Get a user id.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>User id.</returns>
		long GetId(Guid sessionId);
	}
}