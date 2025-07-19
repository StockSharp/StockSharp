# StockSharp Media

Media is a library of images and icons used across the StockSharp platform. It packs logos of various brokers and exchanges as embedded resources so that other StockSharp projects can easily display consistent visuals.

## Contents

- **Logos** – located under the `logos` folder. Includes PNG and SVG files for dozens of trading venues.
- **Product banners** – animated GIFs for applications such as Hydra, Terminal and Shell.
- **Application logo** – `SLogo.png` used in the main README and other places.

The `Media.csproj` project is a .NET `WindowsDesktop` class library that exposes these files as resources. It is referenced by other solutions inside StockSharp to show broker/exchange logos and product banners.

## Generated `MediaNames` class

A companion project `Media.Names` provides a static class `MediaNames` with constants for every file inside the `logos` folder. The class is generated at build time by the `Media.Generator` source generator. This allows you to refer to files by constant names instead of hard‑coding string literals.

```csharp
using System.Reflection;
using System.IO;

// Example of reading a logo from resources
var asm = typeof(MediaNames).Assembly;
using var stream = asm.GetManifestResourceStream($"StockSharp.Media.logos.{MediaNames.binance}");
```

The source generator runs automatically when you build `Media.Names.csproj`. If no logo files are present a diagnostic warning `MEDIA001` is emitted.

## Referencing

To use the icons in your own project:

1. Add a reference to `Media` for the actual resources.
2. Add a reference to `Media.Names` to get the generated constants. The build will require the .NET SDK with WindowsDesktop workload because `Media.csproj` uses `Microsoft.NET.Sdk.WindowsDesktop`.

After referencing you can load any image via `MediaNames` constants as shown above.

