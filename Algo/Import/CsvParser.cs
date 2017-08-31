namespace StockSharp.Algo.Import
{
	using System;
	using System.Collections.Generic;
	using System.IO;
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
					throw new ArgumentOutOfRangeException(nameof(value));

				_skipFromHeader = value;
			}
		}

		/// <summary>
		/// Time zone.
		/// </summary>
		public TimeZoneInfo TimeZone { get; set; }

		/// <summary>
		/// Parse CSV file.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <returns>Parsed instances.</returns>
		public IEnumerable<dynamic> Parse(string fileName, Func<bool> isCancelled = null)
		{
			using (new Scope<TimeZoneInfo>(TimeZone))
			using (var reader = new StreamReader(fileName))
			{
				var columnSeparator = ColumnSeparator.ReplaceIgnoreCase("TAB", "\t");

				var skipLines = SkipFromHeader;
				var lineIndex = 0;
				var fields = Fields.ToArray();

				var enabledFields = fields.Where(f => f.IsEnabled).ToArray();

				fields.ForEach(f => f.Reset());

				while (!reader.EndOfStream)
				{
					if (isCancelled?.Invoke() == true)
						break;

					var line = reader.ReadLine();

					if (skipLines > 0)
					{
						skipLines--;
						continue;
					}

					var cells = line.Split(columnSeparator, false);

					var msgType = DataType.MessageType;

					dynamic instance = CreateInstance(msgType);

					foreach (var field in fields)
					{
						var number = enabledFields.IndexOf(field);

						if (number >= cells.Length)
							throw new InvalidOperationException(LocalizedStrings.Str2869Params.Put(field.DisplayName, number, cells.Length));

						try
						{
							if (number == -1)
							{
								if (field.IsRequired)
									field.ApplyDefaultValue(instance);
							}
							else
								field.ApplyFileValue(instance, cells[number]);
						}
						catch (Exception ex)
						{
							throw new InvalidOperationException(LocalizedStrings.CsvImportError.Put(lineIndex, number, cells[number], field.DisplayName), ex);
						}
					}

					var secMsg = instance as SecurityMessage;

					if (secMsg == null)
					{
						if (instance is ExecutionMessage execMsg)
							execMsg.ExecutionType = (ExecutionTypes)DataType.Arg;

						if (instance is CandleMessage candleMsg)
							candleMsg.State = CandleStates.Finished;
					}
					else if (secMsg.SecurityId.SecurityCode.IsEmpty() || secMsg.SecurityId.BoardCode.IsEmpty())
					{
						if (!IgnoreNonIdSecurities)
							this.AddErrorLog(LocalizedStrings.LineNoSecurityId.Put(line));

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