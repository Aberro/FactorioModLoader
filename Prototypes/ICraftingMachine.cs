using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[Repository("data.raw.crafting-machine")]
	public interface ICraftingMachine : IEntity
	{
		IEnumerable<IRecipeCategory> CraftingCategories { get; }

		[PublicAPI]
		Energy EnergyUsage { get; }

		[PublicAPI]
		[Accessor(typeof(EnergySourceAccessor))]
		IEnergySource EnergySource { get; }

		[PublicAPI]
		[DefaultValue(0.0f)]
		float BaseProductivity { get; }

		[PublicAPI]
		double CraftingSpeed { get; }

		[PublicAPI]
		[DefaultValue(null)]
		[CanBeNull]
		IEnumerable<IFluidBox> FluidBoxes { get; }

		[PublicAPI]
		[DefaultValue(null)]
		[CanBeNull]
		IModuleSpecification ModuleSpecification { get; }

		[PublicAPI]
		[DefaultValue(true)]
		bool ReturnIngredientsOnChange { get; }
	}
}
