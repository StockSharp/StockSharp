# StockSharp Library - Cross-Platform Compatibility

## Overview

StockSharp is designed to be largely cross-platform, thanks to its use of .NET Core. This allows the core functionalities to operate across different operating systems such as Windows, Linux, and macOS. However, not all features and components are cross-platform due to dependencies on platform-specific technologies.

## Cross-Platform Supported Features

- **Core Trading Algorithms**: The algorithms that drive trading strategies are fully cross-platform and can be executed on any system that supports .NET Core.
- **Connectors**: Most of the connectors to trading services, like Binance, are designed to be cross-platform, allowing users to interact with different financial markets.
- **Market Data Handling**: Functions for handling and processing market data are cross-platform, ensuring compatibility across various environments.
- **Order Management**: The order management system is universally applicable, facilitating trade execution and management on any supported platform.

## Non-Cross-Platform Features

- **Graphical User Interfaces (GUIs)**: While the core library is cross-platform, graphical interfaces, particularly those built with Windows Presentation Foundation (WPF), are only supported on Windows. Alternative interfaces would need to be implemented using cross-platform GUI frameworks like Avalonia or UNO Platform for use on other systems.
- **Certain Connectors**: Some connectors may rely on Windows-specific libraries or APIs, limiting their use to Windows environments only.
- **Performance Tools**: Some performance optimization tools and features might be designed with specific platforms in mind, primarily Windows, due to the original development and testing environments.

## Recommendations for Cross-Platform Deployment

- **Use Docker or Virtualization**: For ensuring the best compatibility and to simulate a uniform environment across different systems, consider deploying your StockSharp application within Docker containers.
- **Alternative GUI Frameworks**: For developing a truly cross-platform GUI, consider using frameworks like Avalonia for .NET, which supports a wide range of operating systems while maintaining a consistent user experience.

## Conclusion

StockSharp's core functionality provides significant flexibility for developing and deploying trading strategies on various platforms. However, for full cross-platform support, especially in GUIs and certain extensions, additional considerations and developments may be necessary.
