namespace StockSharp.Algo.Import
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Messages parser from text file in CSV format.
	/// </summary>
	public class CsvParser : BaseLogReceiver
	{
		/// <summary>
		/// Data type info.
		/// </summary>
		public DataType DataType { get; }

		/// <summary>
		/// Importing fields.
		/// </summary>
		public IEnumerable<FieldMapping> Fields { get; }

		/// <summary>
		/// Extended info <see cref="Message.ExtensionInfo"/> storage.
		/// </summary>
		public IExtendedInfoStorageItem ExtendedInfoStorageItem { get; set; }

		/// <summary>
		/// Ignore securities without identifiers.
		/// </summary>
		public bool IgnoreNonIdSecurities { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvParser"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="fields">Importing fields.</param>
		public CsvParser(DataType dataType, IEnumerable<FieldMapping> fields)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (fields == null)
				throw new ArgumentNullException(nameof(fields));

			if (dataType.MessageType == null)
				throw new ArgumentException(nameof(dataType));

			DataType = dataType;
			Fields = fields.ToArray();

			if (Fields.IsEmpty())
				throw new ArgumentException(nameof(fields));
		}

		private string _columnSeparator = ",";

		/// <summary>
		/// Column separator. Tabulation is denoted by TAB.
		/// </summary>
		public string ColumnSeparator
		{
			get => _columnSeparator;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_columnSeparator = value;
			}
		}

		private int _skipFromHeader;

		/// <summary>
		/// Number of lines to be skipped from the beginning of the file (if they contain meta information).
		/// </summary>
		public int SkipFromHeader
		{
			get => _skipFromHeader;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_skipFromHeader = value;
			}
		}

		private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

		/// <summary>
		/// Time zone.
		/// </summary>
		public TimeZoneInfo TimeZone
		{
			get => _timeZone;
			set => _timeZone = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Parse CSV file.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <returns>Parsed instances.</returns>
		public IEnumerable<dynamic> Parse(string fileName, Func<bool> isCancelled = null)
		{
			var columnSeparator = ColumnSeparator.ReplaceIgnoreCase("TAB", "\t");

			using (new Scope<TimeZoneInfo>(TimeZone))
			using (var reader = new CsvFileReader(fileName) { Delimiter = columnSeparator[0] })
			{
				var skipLines = SkipFromHeader;
				var lineIndex = 0;

				var fields = Fields.ToArray();

				fields.ForEach(f => f.Reset());

				var cells = new List<string>();

				while (reader.ReadRow(cells))
				{
					if (isCancelled?.Invoke() == true)
						break;

					if (skipLines > 0)
					{
						skipLines--;
						continue;
					}

					var msgType = DataType.MessageType;

					dynamic instance = CreateInstance(msgType);

					foreach (var field in fields)
					{
						if (field.Order >= cells.Count)
							throw new InvalidOperationException(LocalizedStrings.Str2869Params.Put(field.DisplayName, field.Order, cells.Count));

						try
						{
							if (field.Order == null)
							{
								if (field.IsRequired)
									field.ApplyDefaultValue(instance);
							}
							else
								field.ApplyFileValue(instance, cells[field.Order.Value]);
						}
						catch (Exception ex)
						{
							throw new InvalidOperationException(LocalizedStrings.CsvImportError.Put(lineIndex, field.Order, field.Order == null ? "NULL" : cells[field.Order.Value], field.DisplayName), ex);
						}
					}

					if (!(instance is SecurityMessage secMsg))
					{
						if (instance is ExecutionMessage execMsg)
							execMsg.ExecutionType = (ExecutionTypes)DataType.Arg;

						if (instance is CandleMessage candleMsg)
							candleMsg.State = CandleStates.Finished;
					}
					else if (secMsg.SecurityId.SecurityCode.IsEmpty() || secMsg.SecurityId.BoardCode.IsEmpty())
					{
						if (!IgnoreNonIdSecurities)
							this.AddErrorLog(LocalizedStrings.LineNoSecurityId.Put(reader.CurrLine));

						continue;
					}

					yield return instance;
				}
			}
		}

		/// <summary>
		/// Create instance for the specified type.
		/// </summary>
		/// <param name="msgType">Message type.</param>
		/// <returns>Instance.</returns>
		protected virtual object CreateInstance(Type msgType)
		{
			var instance = msgType == typeof(QuoteChangeMessage)
				? new TimeQuoteChange()
				: msgType.CreateInstance<object>();

			if (msgType == typeof(SecurityMessage) && ExtendedInfoStorageItem != null)
				((SecurityMessage)instance).ExtensionInfo = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

			return instance;
		}
	}
}