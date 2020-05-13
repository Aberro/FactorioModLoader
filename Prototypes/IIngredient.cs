using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Builder(typeof(IngredientBuilder))]
	public interface IIngredient
	{
	}

	[PublicAPI]
	public interface IItemIngredient : IIngredient
	{
		[PublicAPI]
		[Alias("name")]
		[Indexed(0)]
		public IItem Item { get; }

		[PublicAPI]
		[Indexed(1)]
		public ushort Amount { get; }

		[PublicAPI]
		public ushort CatalystAmount { get; }
	}

	[PublicAPI]
	public interface IFluidIngredient : IIngredient
	{
		[PublicAPI]
		[Alias("name")]
		IFluid Fluid { get; }

		[PublicAPI]
		double Amount { get; }

		[PublicAPI]
		double? Temperature { get; }

		[PublicAPI]
		double? MinimumTemperature { get; }

		[PublicAPI]
		double? MaximumTemperature { get; }

		[PublicAPI]
		[DefaultValue(0)]
		double CatalystAmount { get; }

		[PublicAPI]
		[DefaultValue(0)]
		uint FluidboxIndex { get; }
	}
}