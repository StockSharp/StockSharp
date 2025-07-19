# StockSharp Media.Names

Media.Names is a lightweight utility library that provides strongly typed access to the names of media assets used throughout the StockSharp ecosystem. It contains a single static class, `MediaNames`, whose members correspond to the files located in the [`Media`](../Media) project under the `logos` directory.

## How It Works

The actual constant fields in `MediaNames` are generated at build time by the [`MediaNamesGenerator`](../Media.Generator/MediaNamesGenerator.cs) Roslyn source generator. During compilation the generator scans the `Media/logos` folder for all `.png` and `.svg` files and then emits a partial definition of the `MediaNames` class. Each logo file becomes a `public const string` field whose value is the file name. This approach ensures that the set of available media names always stays in sync with the images embedded in `StockSharp.Media`.

## Why Use Media.Names?

Referencing image files by string literal is error‑prone. Media.Names eliminates typos by offering compile‑time constants. These constants are commonly used with the [`MediaIconAttribute`](../Localization/MediaIconAttribute.cs) to associate an icon with a connector or other component:

```csharp
[MediaIcon(Media.MediaNames.binance)]
public class BinanceMessageAdapter { /* ... */ }
```

Because the constants are generated automatically, you never need to edit the file manually when new logos are added.

## Building

Media.Names targets **.NET Standard 2.0** and has no runtime dependencies aside from the generated source. To build the project locally run:

```bash
dotnet build Media.Names/Media.Names.csproj
```

The build will restore the `Media.Generator` project, execute the source generator, and produce `StockSharp.Media.Names.dll` along with an XML documentation file.

## Adding New Icons

1. Place the `.png` or `.svg` file into the [`Media/logos`](../Media/logos) directory of the repository.
2. Build the solution. The `MediaNamesGenerator` will pick up the new file and add a corresponding constant to `MediaNames`.

