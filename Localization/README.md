# StockSharp.Localization

## Overview

`StockSharp.Localization` provides a flexible and extensible localization system for the entire StockSharp framework. The package contains the default English resources and utilities for switching languages at runtime. It works together with language specific packages located in `Localization.Langs`.

## Features

- **JSON based resources** – human readable `strings.json` holds the default English text for all UI elements and messages.
- **Source generator** – during build `Localization.Generator` converts the JSON file into strongly typed properties of the `LocalizedStrings` class.
- **Runtime language switching** – any number of language packs can be added and activated through `LocalizedStrings.AddLanguage` and `LocalizedStrings.ActiveLanguage`.
- **Missing translation tracking** – the `Missing` event notifies when a resource key or text does not have a translation.
- **Automatic culture update** – setting `ActiveLanguage` updates `Thread.CurrentCulture` and `Thread.CurrentUICulture`.

## Installation

Add `StockSharp.Localization` as a NuGet package to your project. To include additional languages, reference the corresponding package such as `StockSharp.Localization.ru` or the meta package `StockSharp.Localization.All`.

The source generator is included automatically and requires no manual configuration.

## Usage

Retrieve a localized string via the generated properties:

```csharp
// get text for the current language
string text = LocalizedStrings.About;

// explicitly translate from one language to another
string russian = "About".Translate(from: LocalizedStrings.EnCode, to: LocalizedStrings.RuCode);
```

Switch the active language at runtime:

```csharp
// change UI culture to Russian
LocalizedStrings.ActiveLanguage = LocalizedStrings.RuCode;
```

## Extending Language Support

To add your own language:

1. Create a `strings.json` file with translations where keys match those in the base project.
2. Include the JSON in a new `.csproj` referencing `common_lang.props` (see examples in `Localization.Langs`).
3. Reference the resulting assembly in your application. `LocalizedStrings` will automatically pick it up when available.

Languages can also be loaded dynamically using `AddLanguage(string langCode, Stream stream)` at runtime.


