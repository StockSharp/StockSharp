namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, that save <see cref="Message.ExtensionInfo"/> into <see cref="IExtendedInfoStorage"/>.
	/// </summary>
	public class ExtendedInfoStorageMessageAdapter : MessageAdapterWrapper
	{
		private readonly IExtendedInfoStorage _extendedInfoStorage;
		private readonly string _storageName;
		private readonly Tuple<string, Type>[] _fields;

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageAdapterWrapper"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="extendedInfoStorage">Extended info <see cref="Message.ExtensionInfo"/> storage.</param>
		/// <param name="storageName">Storage name.</param>
		/// <param name="fields">Extended fields (names and types).</param>
		public ExtendedInfoStorageMessageAdapter(IMessageAdapter innerAdapter, IExtendedInfoStorage extendedInfoStorage, string storageName, Tuple<string, Type>[] fields)
			: base(innerAdapter)
		{
			_extendedInfoStorage = extendedInfoStorage;
			_storageName = storageName;
			_fields = fields;
		}

		private readonly SyncObject _sync = new SyncObject();
		private IExtendedInfoStorageItem _storage;

		private IExtendedInfoStorageItem GetStorage()
		{
			if (_storage == null)
			{
				lock (_sync)
				{
					if (_storage == null)
						_storage = _extendedInfoStorage.Create(_storageName, _fields);
				}	
			}

			return _storage;
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (!message.IsBack)
			{
				var secMsg = message as SecurityMessage;

				if (secMsg?.ExtensionInfo != null)
					GetStorage().Add(secMsg.SecurityId, secMsg.ExtensionInfo);
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="ExtendedInfoStorageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new ExtendedInfoStorageMessageAdapter(InnerAdapter, _extendedInfoStorage, _storageName, _fields);
		}
	}
}