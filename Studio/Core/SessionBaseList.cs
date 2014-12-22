namespace StockSharp.Studio.Core
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Algo.Storages;

	public class SessionBaseList<T> : BaseStorageEntityList<T>
		where T : class
	{
		public SessionBaseList(IStorage storage, Session session)
			: base(storage)
		{
			if (session == null)
				throw new ArgumentNullException("session");

			Session = session;
		}

		protected Session Session { get; private set; }
	}
}