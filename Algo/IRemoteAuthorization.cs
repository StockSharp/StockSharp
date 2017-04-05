namespace StockSharp.Algo
{
	using System;

	using Ecng.Security;

	using StockSharp.Messages;

	/// <summary>
	/// The interface describing the connection access check module.
	/// </summary>
	public interface IRemoteAuthorization : IAuthorization
	{
		/// <summary>
		/// Get permission for request.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="requiredPermissions">Required permissions.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="date">Date.</param>
		/// <returns>Possible permissions.</returns>
		bool HasPermissions(Guid sessionId, UserPermissions requiredPermissions, string securityId, string dataType, object arg, DateTime? date);
	}

	/// <summary>
	/// The connection access check module which provides access to all connections.
	/// </summary>
	public class AnonymousRemoteAuthorization : AnonymousAuthorization, IRemoteAuthorization
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AnonymousRemoteAuthorization"/>.
		/// </summary>
		public AnonymousRemoteAuthorization()
		{
		}

		/// <summary>
		/// Get permission for request.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="requiredPermissions">Required permissions.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="date">Date.</param>
		/// <returns>Possible permissions.</returns>
		public virtual bool HasPermissions(Guid sessionId, UserPermissions requiredPermissions, string securityId, string dataType, object arg, DateTime? date)
		{
			switch (requiredPermissions)
			{
				case UserPermissions.Save:
				case UserPermissions.Delete:
				case UserPermissions.EditSecurities:
				case UserPermissions.EditExchanges:
				case UserPermissions.EditBoards:
				case UserPermissions.DeleteSecurities:
				case UserPermissions.DeleteExchanges:
				case UserPermissions.DeleteBoards:
					return false;
				case UserPermissions.Load:
				case UserPermissions.SecurityLookup:
				case UserPermissions.ExchangeLookup:
				case UserPermissions.ExchangeBoardLookup:
					return true;
				default:
					throw new ArgumentOutOfRangeException(nameof(requiredPermissions), requiredPermissions, null);
			}
		}
	}

	///// <summary>
	///// The connection to <see cref="RemoteStorage"/> access check module based on Windows authentication.
	///// </summary>
	//public class WindowsRemoteStorageAuthorization : WindowsAuthorization, IRemoteStorageAuthorization
	//{
	//	/// <summary>
	//	/// Initializes a new instance of the <see cref="WindowsRemoteStorageAuthorization"/>.
	//	/// </summary>
	//	public WindowsRemoteStorageAuthorization()
	//	{
	//	}

	//	/// <summary>
	//	/// Get permission for request.
	//	/// </summary>
	//	/// <param name="sessionId">Session ID.</param>
	//	/// <param name="requiredPermissions">Required permissions.</param>
	//	/// <param name="securityId">Security ID.</param>
	//	/// <param name="dataType">Market data type.</param>
	//	/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
	//	/// <param name="date">Date.</param>
	//	/// <returns>Possible permissions.</returns>
	//	public bool HasPermissions(Guid sessionId, UserPermissions requiredPermissions, string securityId, string dataType, object arg, DateTime? date)
	//	{
	//		switch (requiredPermissions)
	//		{
	//			case UserPermissions.Save:
	//			case UserPermissions.Delete:
	//			case UserPermissions.SecuritySave:
	//			case UserPermissions.ExchangeSave:
	//			case UserPermissions.ExchangeBoardSave:
	//			case UserPermissions.SecurityDelete:
	//			case UserPermissions.ExchangeDelete:
	//				return false;
	//			case UserPermissions.Load:
	//			case UserPermissions.SecurityLookup:
	//			case UserPermissions.ExchangeLookup:
	//			case UserPermissions.ExchangeBoardLookup:
	//				return true;
	//			default:
	//				throw new ArgumentOutOfRangeException(nameof(requiredPermissions), requiredPermissions, null);
	//		}
	//	}
	//}

	///// <summary>
	///// The connection to <see cref="RemoteStorage"/> access check module based on the <see cref="IAuthenticationService"/> authentication.
	///// </summary>
	//public class CommunityRemoteStorageAuthorization : CommunityAuthorization, IRemoteStorageAuthorization
	//{
	//	/// <summary>
	//	/// Initializes a new instance of the <see cref="CommunityRemoteStorageAuthorization"/>.
	//	/// </summary>
	//	public CommunityRemoteStorageAuthorization()
	//	{
	//	}

	//	/// <summary>
	//	/// Get permission for request.
	//	/// </summary>
	//	/// <param name="sessionId">Session ID.</param>
	//	/// <param name="requiredPermissions">Required permissions.</param>
	//	/// <param name="securityId">Security ID.</param>
	//	/// <param name="dataType">Market data type.</param>
	//	/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
	//	/// <param name="date">Date.</param>
	//	/// <returns>Possible permissions.</returns>
	//	public bool HasPermissions(Guid sessionId, UserPermissions requiredPermissions, string securityId, string dataType, object arg, DateTime? date)
	//	{
	//		switch (requiredPermissions)
	//		{
	//			case UserPermissions.Save:
	//			case UserPermissions.Delete:
	//			case UserPermissions.SecuritySave:
	//			case UserPermissions.ExchangeSave:
	//			case UserPermissions.ExchangeBoardSave:
	//			case UserPermissions.SecurityDelete:
	//			case UserPermissions.ExchangeDelete:
	//				return false;
	//			case UserPermissions.Load:
	//			case UserPermissions.SecurityLookup:
	//			case UserPermissions.ExchangeLookup:
	//			case UserPermissions.ExchangeBoardLookup:
	//				return true;
	//			default:
	//				throw new ArgumentOutOfRangeException(nameof(requiredPermissions), requiredPermissions, null);
	//		}
	//	}
	//}
}