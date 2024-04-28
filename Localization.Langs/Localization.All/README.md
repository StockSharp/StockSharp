# StockSharp.Localization.All

## Overview

`StockSharp.Localization.All` is a comprehensive localization package designed exclusively for distribution via NuGet. This package simplifies the process of integrating multiple language supports into your projects by acting as a single reference point for all localization needs associated with the StockSharp framework.

## Purpose

The primary goal of this package is to streamline the development process. Instead of adding individual references to each language pack needed for your application, `StockSharp.Localization.All` allows you to include one dependency that automatically brings in all other necessary language packages through its dependencies. This ensures that your project setup is cleaner and managing updates becomes much easier.

## Usage

To use `StockSharp.Localization.All`, simply add it as a NuGet package to your project. Once added, it will automatically handle the inclusion of all other language-specific packages required by your StockSharp application.

### Installation

You can install the package using the NuGet Package Manager:

```cmd
Install-Package StockSharp.Localization.All
```

## Benefits

- **Simplicity**: Reduces the complexity of your project dependencies by encapsulating all StockSharp localization resources into a single package.
- **Maintainability**: Updates to language packages are centrally managed, ensuring that you always have the latest translations with minimal effort.
- **Scalability**: Easily scale your application to support new languages by relying on the comprehensive coverage of `StockSharp.Localization.All`.

## Conclusion

`StockSharp.Localization.All` is an essential tool for developers working with the StockSharp framework who need to support multiple languages. By using this package, developers can significantly reduce the overhead associated with managing multiple language packs and ensure their applications are ready for a global audience.

For more information about StockSharp and other related packages, please visit [StockSharp's official website](https://stocksharp.com).