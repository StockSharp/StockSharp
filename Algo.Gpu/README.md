# StockSharp Algo.Gpu Library

## Overview

StockSharp.Algo.Gpu is a specialized library that provides GPU-accelerated calculations for technical indicators using the [ILGPU](https://github.com/m4rs-mt/ILGPU) framework. It enables massive parallel processing of market data indicators on CUDA, OpenCL, and CPU accelerators, offering significant performance improvements for batch calculations and strategy optimization scenarios.

## Features

- **GPU Acceleration** – leverages ILGPU for cross-platform GPU computing with support for CUDA, OpenCL, and CPU backends
- **Batch Processing** – efficiently processes multiple data series and parameter combinations in a single GPU pass
- **Optimized Data Structures** – specialized `GpuCandle` and `GpuIndicatorResult` structs designed for GPU memory layout
- **Technical Indicators** – currently includes GPU-accelerated Simple Moving Average (SMA), Average Directional Index (ADX), and Envelope with extensible architecture for additional indicators
- **Automatic Device Selection** – intelligent accelerator selection based on device capabilities and memory size

## Getting Started

### Prerequisites

- [.NET SDK 6.0](https://dotnet.microsoft.com/) or later
- GPU with CUDA support, OpenCL drivers, or modern CPU for fallback processing
- Visual Studio 2022 or any compatible IDE

### Basic Usage

The typical workflow involves creating an accelerator, setting up input data, and running batch calculations:

```csharp
using StockSharp.Algo.Gpu;
using StockSharp.Algo.Gpu.Indicators;
using StockSharp.Algo.Indicators;

// Create best available accelerator (CUDA > OpenCL > CPU)
var (context, accelerator) = GpuAcceleratorFactory.CreateBestAccelerator();

// Convert candles to GPU format
var gpuCandles = candles.Select(c => new GpuCandle(
    c.OpenTime, c.OpenPrice, c.HighPrice, c.LowPrice, c.ClosePrice, c.TotalVolume
)).ToArray();

// Define SMA parameters to test
var smaParams = new GpuSmaParams[]
{
    new(10, (byte)Level1Fields.ClosePrice),
    new(20, (byte)Level1Fields.ClosePrice),
    new(50, (byte)Level1Fields.ClosePrice)
};

// Calculate SMA for multiple parameter sets
using var calculator = new GpuSmaCalculator(context, accelerator);
IGpuIndicatorResult[][][] gpuResults = calculator.Calculate(new[] { gpuCandles }, smaParams);

// Convert GPU results to indicator values
var smaIndicator = new SimpleMovingAverage { Length = 20 };
foreach (var seriesResult in gpuResults[0])
{
    foreach (IGpuIndicatorResult gpuResult in seriesResult)
    {
        if (gpuResult.GetIsFormed())
        {
            var indicatorValue = gpuResult.ToValue(smaIndicator);
            Console.WriteLine($"SMA: {indicatorValue.ToDecimal()} at {indicatorValue.Time}");
        }
    }
}

// Cleanup
accelerator.Dispose();
context.Dispose();
```

## Architecture

### Core Components

- **`GpuAcceleratorFactory`** – factory for creating and selecting optimal ILGPU accelerators
- **`GpuIndicatorCalculatorBase`** – base class for all GPU indicator calculators
- **`GpuCandle`** – GPU-optimized candle data structure with float precision for performance
- **`IGpuIndicatorResult`** – interface for GPU indicator calculation results with ToValue conversion
- **`GpuIndicatorResult`** – standard GPU result structure implementing IGpuIndicatorResult

### Indicators

- **`GpuSmaCalculator`** – Simple Moving Average calculator supporting multiple price types and batch processing
- **`GpuAdxCalculator`** – Average Directional Index calculator with smoothed directional movement results
- **`GpuEnvelopeCalculator`** – Envelope calculator providing middle, upper, and lower band outputs

### Data Types

- **`GpuSmaParams`** – parameter structure for SMA calculations with Level1Fields price type support
- **`GpuAdxParams`** – parameter structure for ADX calculations including Wilder smoothing parameters
- **`GpuEnvelopeParams`** – parameter structure for Envelope calculations with shift and price type configuration
- **`IGpuIndicatorParams`** – interface for GPU indicator parameter structures
- **`GpuAdxResult`** – complex GPU result structure for ADX (+DI, -DI, ADX) outputs
- **`GpuEnvelopeResult`** – complex GPU result structure returning middle, upper, and lower band values
- **`IGpuIndicatorResult`** – interface for GPU calculation results with ToValue() conversion method

## Performance Considerations

- GPU calculations use single-precision floating-point (`float`) for optimal performance
- Batch processing is most efficient when processing multiple series or parameter combinations simultaneously
- Memory transfers between CPU and GPU are minimized through batched operations
- Automatic device selection prioritizes CUDA over OpenCL over CPU for best performance

## Use Cases

- **Strategy Optimization** – testing multiple indicator parameters across large datasets
- **Historical Analysis** – processing years of market data with various technical indicators