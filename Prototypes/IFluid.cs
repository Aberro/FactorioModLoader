#nullable enable
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.fluid")]
	public interface IFluid : IPrototypeBase
	{
		[PublicAPI]
		IconSpecification Icon { get; }

		[PublicAPI]
		[DefaultValue("fluid")]
		IItemSubgroup Subgroup { get; }

		[PublicAPI]
		[DefaultValue(1.0)]
		double EmissionMultiplier { get; }

		[PublicAPI]
		[DefaultValue(0.0)]
		double FuelValue { get; }

		[PublicAPI]
		[DefaultValue(double.PositiveInfinity)]
		double GasTemperature { get; }

		[PublicAPI]
		[DefaultValue("1KJ")]
		Energy HeatCapacity { get; }

		[PublicAPI]
		[DefaultValue(double.PositiveInfinity)]
		double MaxTemperature { get; }

		[PublicAPI]
		[DefaultValue(false)]
		bool Hidden { get; }
	}
}