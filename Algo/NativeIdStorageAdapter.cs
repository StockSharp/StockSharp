namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Security native identifier storage.
	/// </summary>
	public interface INativeIdStorage
	{
		/// <summary>
		/// Save native identifiers for adapter.
		/// </summary>
		/// <param name="adapter">Message adapter.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="nativeId">Native security identifier.</param>
		void Save(IMessageAdapter adapter, SecurityId securityId, object nativeId);

		/// <summary>
		/// Load native identifiers for adapter. 
		/// </summary>
		/// <param name="adapter">Message adapter.</param>
		/// <returns>Identifiers.</returns>
		IEnumerable<Tuple<SecurityId, object>> Load(IMessageAdapter adapter);
	}

	/// <summary>
	/// Csv security native identifier storage.
	/// </summary>
	public sealed class CsvNativeIdStorage : INativeIdStorage
	{
		private readonly string _fileName;
		private readonly Dictionary<string, Dictionary<SecurityId, object>> _nativeIds = new Dictionary<string, Dictionary<SecurityId, object>>();

		/// <summary>
		/// Create <see cref="CsvNativeIdStorage"/>.
		/// </summary>
		/// <param name="fileName">Storage file name.</param>
		public CsvNativeIdStorage(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));

			_fileName = fileName;

			Load();
		}

		/// <summary>
		/// Save native identifiers for adapter.
		/// </summary>
		/// <param name="adapter">Message adapter.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="nativeId">Native security identifier.</param>
		public void Save(IMessageAdapter adapter, SecurityId securityId, object nativeId)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			if (nativeId == null)
				throw new ArgumentNullException(nameof(nativeId));

			var name = adapter.GetType().Name;
			var nativeIds = _nativeIds.SafeAdd(name);

			nativeIds[securityId] = nativeId;

			Save();
		}

		/// <summary>
		/// Load native identifiers for adapter. 
		/// </summary>
		/// <param name="adapter">Message adapter.</param>
		/// <returns>Identifiers.</returns>
		public IEnumerable<Tuple<SecurityId, object>> Load(IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			var name = adapter.GetType().Name;
			var nativeIds = _nativeIds.TryGetValue(name);

			if (nativeIds == null)
				return Enumerable.Empty<Tuple<SecurityId, object>>();

			return nativeIds.Select(p => Tuple.Create(p.Key, p.Value)).ToArray();
		}

		private void Save()
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				try
				{
					using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
					using (var writer = new CsvFileWriter(stream))
					{
						foreach (var pair in _nativeIds)
						{
							foreach (var item in pair.Value)
							{
								var securityId = item.Key;
								var nativeId = item.Value;

								writer.WriteRow(new[]
								{
								pair.Key,
								securityId.SecurityCode,
								securityId.BoardCode,
								nativeId.GetType().GetTypeName(false),
								nativeId.ToString()
							});
							}
						}
					}
				}
				catch (Exception excp)
				{
					excp.LogError("Save native storage to {0} error.".Put(_fileName));
				}
			});
		}

		private void Load()
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				try
				{
					using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
					{
						var reader = new FastCsvReader(stream, Encoding.UTF8);

						while (reader.NextLine())
						{
							var name = reader.ReadString();
							var nativeIds = _nativeIds.SafeAdd(name);

							var securityId = new SecurityId
							{
								SecurityCode = reader.ReadString(),
								BoardCode = reader.ReadString()
							};

							var type = reader.ReadString().To<Type>();
							var nativeId = reader.ReadString().To(type);

							nativeIds[securityId] = nativeId;
						}
					}
				}
				catch (Exception excp)
				{
					excp.LogError("Load native storage from {0} error.".Put(_fileName));
				}
			});
        }
	}

	/// <summary>
	/// Native Id message adapter.
	/// </summary>
	public class NativeIdStorageAdapter : MessageAdapterWrapper
	{
		private sealed class InMemoryStorage : INativeIdStorage
		{
			private readonly Dictionary<SecurityId, object> _nativeIds = new Dictionary<SecurityId, object>();

			public void Save(IMessageAdapter adapter, SecurityId securityId, object nativeId)
			{
				if (adapter == null)
					throw new ArgumentNullException(nameof(adapter));

				if (nativeId == null)
					throw new ArgumentNullException(nameof(nativeId));

				_nativeIds[securityId] = nativeId;
			}

			public IEnumerable<Tuple<SecurityId, object>> Load(IMessageAdapter adapter)
			{
				if (adapter == null)
					throw new ArgumentNullException(nameof(adapter));

				return _nativeIds.Select(p => Tuple.Create(p.Key, p.Value)).ToArray();
			}
		}

		/// <summary>
		/// Native ids storage.
		/// </summary>
		public INativeIdStorage Storage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NativeIdStorageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public NativeIdStorageAdapter(IMessageAdapter innerAdapter)
			: this(innerAdapter, new InMemoryStorage())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NativeIdStorageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="storage">Native ids storage.</param>
		public NativeIdStorageAdapter(IMessageAdapter innerAdapter, INativeIdStorage storage)
			: base(innerAdapter)
		{
			Storage = storage;
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					base.OnInnerAdapterNewOutMessage(message);

					var nativeIds = Storage.Load(InnerAdapter);

					foreach (var tuple in nativeIds)
					{
						var securityId = tuple.Item1;
						var nativeId = tuple.Item2;

						securityId.Native = nativeId;

						base.OnInnerAdapterNewOutMessage(new SecurityMessage { SecurityId = securityId });
					}

					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					var securityId = secMsg.SecurityId;

					var nativeSecurityId = securityId.Native;
					var securityCode = securityId.SecurityCode;
					var boardCode = securityId.BoardCode;

					if (securityCode.IsEmpty() || boardCode.IsEmpty())
						throw new InvalidOperationException();

					if (nativeSecurityId != null)
					{
						var temp = securityId;
						// GetHashCode shouldn't calc based on native id
						temp.Native = null;

						Storage.Save(InnerAdapter, temp, nativeSecurityId);
					}

					base.OnInnerAdapterNewOutMessage(message);
					break;
				}

				default:
					base.OnInnerAdapterNewOutMessage(message);
					break;
			}
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new NativeIdStorageAdapter(InnerAdapter, Storage);
		}
	}
}