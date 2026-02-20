namespace StockSharp.Algo.Gpu;

/// <summary>
/// Factory utilities to select and create an ILGPU accelerator.
/// </summary>
public static class GpuAcceleratorFactory
{
	/// <summary>
	/// Try to create the best available accelerator using a fresh ILGPU <see cref="Context"/>.
	/// </summary>
	/// <param name="context">Created ILGPU context when method returns <see langword="true"/>. The caller must dispose it.</param>
	/// <param name="accelerator">Created accelerator when method returns <see langword="true"/>. The caller must dispose it.</param>
	/// <returns><see langword="true"/> if an accelerator was created; otherwise, <see langword="false"/>.</returns>
	public static bool TryCreateBestAccelerator(out Context context, out Accelerator accelerator)
	{
		context = Context.Create(b => b.Default().EnableAlgorithms());

		Device best = null;
		var bestScore = long.MinValue;

		foreach (var dev in context)
		{
			var typeScore = dev.AcceleratorType switch
			{
				AcceleratorType.Cuda => 3,
				AcceleratorType.OpenCL => 2,
				AcceleratorType.CPU => 1,
				_ => 0,
			};

			var score = typeScore * 1_000_000_000_000L + dev.MemorySize;
			if (score > bestScore)
			{
				bestScore = score;
				best = dev;
			}
		}

		if (best is null)
		{
			context.Dispose();
			accelerator = null;
			return false;
		}

		accelerator = best.CreateAccelerator(context);
		return true;
	}

	/// <summary>
	/// Create the best available accelerator using a fresh ILGPU <see cref="Context"/>.
	/// </summary>
	/// <returns>Tuple containing created <see cref="Context"/> and <see cref="Accelerator"/>.</returns>
	/// <exception cref="NotSupportedException">Thrown when no accelerators are available.</exception>
	public static (Context Context, Accelerator Accelerator) CreateBestAccelerator()
	{
		if (!TryCreateBestAccelerator(out var ctx, out var acc))
			throw new NotSupportedException("ILGPU: no accelerators available on this machine.");

		return (ctx, acc);
	}
}