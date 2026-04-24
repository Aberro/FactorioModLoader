#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	public enum ProductionType
	{
		None,
		Input,
		InputOutput,
		Output,
	}
	[PublicAPI]
	public interface IFluidBox
	{
		[PublicAPI]
		[DefaultValue(1.0)]
		double BaseArea { get; }

		[PublicAPI]
		[DefaultValue(0.0)]
		double BaseLevel { get; }
		
		[PublicAPI]
		[DefaultValue(1)]
		double Height { get; }

		[PublicAPI]
		[DefaultValue(null)]
		IFluid? Filter { get; }

		[PublicAPI]
		[DefaultValue(null)]
		double? MinimumTemperature { get; }

		[PublicAPI]
		[DefaultValue(null)]
		double? MaximumTemperature { get; }

		[PublicAPI]
		[DefaultValue(ProductionType.None)]
		ProductionType ProductionType { get; }
	}
}
