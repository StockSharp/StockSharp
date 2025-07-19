# S#.Algo.Export

`S#.Algo.Export` is a module of the StockSharp trading platform responsible for exporting market data and trading information to a variety of external formats. The library converts incoming `Message` based data structures into files or databases so it can be consumed by third‑party tools.

## Features

- **Multiple output formats**: export can be performed to Excel spreadsheets, text files, JSON or XML documents, databases via LinqToDB, and to StockSharp's own storage format.
- **Data type agnostic**: any message type from `StockSharp.Messages` can be exported (ticks, order books, level1, candles, transactions, etc.). The target data type is described by `DataType` and handled in the base exporter logic.
- **Extensible Excel engine**: the `IExcelWorkerProvider` interface abstracts work with Excel files. `DevExpExcelWorkerProvider` implements this interface via DevExpress components and allows both creation of new documents and update of existing ones.
- **Template based text export**: the `TemplateTxtRegistry` class stores default line templates for various message types which are used by `TextExporter` when writing text or CSV files.
- **Database integration**: `DatabaseExporter` uses `LinqToDB` to create tables on the fly and write messages in batches. The exporter can automatically map message fields and manage data uniqueness checking.
- **StockSharp storage**: `StockSharpExporter` writes messages to the native persistent storage using `IStorageRegistry` and `IMarketDataDrive`.
- **Progress control**: every exporter accepts a `Func<int,bool>` delegate used to report element count and abort processing if required.

## Usage

Below is a simplified example of exporting trades to JSON:

```csharp
var exporter = new JsonExporter(DataType.Ticks, i => false, "trades.json")
{
    Indent = true
};

var result = exporter.Export(tradeMessages);
Console.WriteLine($"Saved {result.Item1} trades up to {result.Item2}");
```

Other exporters (`ExcelExporter`, `XmlExporter`, `TextExporter`, `DatabaseExporter`, `StockSharpExporter`) expose the same `Export` method but write to their respective targets. Creation parameters vary according to the destination (file name, Excel provider, database connection, etc.).

## Text Templates

`TemplateTxtRegistry` defines a set of default string templates which can be customized. They describe how message fields are converted to text. For instance, `TemplateTxtTick` controls how each trade is written to a text file. See the source for the full list of templates and examples.


