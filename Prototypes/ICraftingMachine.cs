using System;
using System.Collections.Generic;
using System.Text;

namespace FactorioModLoader.Prototypes
{
	[Repository("data.raw.crafting-machine")]
	public interface ICraftingMachine : IEntity
	{
		IEnumerable<IRecipeCategory> CraftingCategories { get; }
		//IEnergySource EnergySource { get; }
		//IEnumerable<FluidBox> FluidBoxes { get; }
		Energy EnergyUsage { get; }
	}
}
