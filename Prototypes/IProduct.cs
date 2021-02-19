#nullable enable
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Builder(typeof(ProductBuilder))]
	public interface IProduct
	{
		[PublicAPI]
		[DefaultValue(true)]
		bool ShowDetailsInRecipeTooltip { get; }
	}
	[PublicAPI]
	public interface IItemProduct : IProduct
	{
		[PublicAPI]
		[Alias("name")]
		[Indexed(0)]
		IItem Item { get; }

		[PublicAPI]
		[Indexed(1)]
		[DefaultValue(null)]
		ushort? Amount { get; }

		[PublicAPI]
		[DefaultValue(1.0)]
		double Probability { get; }

		[PublicAPI]
		[DefaultValue(null)]
		ushort? AmountMin { get; }

		[PublicAPI]
		[DefaultValue(null)]
		ushort? AmountMax { get; }

		[PublicAPI]
		[DefaultValue(0)]
		ushort CatalystAmount { get; }
	}

	[PublicAPI]
	public interface IFluidProduct : IProduct
	{
		[PublicAPI]
		[Alias("name")]
		IFluid Fluid { get; }

		[PublicAPI]
		[DefaultValue(1.0)]
		double Probability { get; }

		[PublicAPI]
		double? Amount { get; }

		[PublicAPI]
		double? AmountMin { get; }

		[PublicAPI]
		double? AmountMax { get; }

		[PublicAPI]
		double Temperature { get; }

		[PublicAPI]
		[DefaultValue(0)]
		double CatalystAmount { get; }

		[PublicAPI]
		[DefaultValue(0)]
		uint FluidboxIndex { get; }
	}
}