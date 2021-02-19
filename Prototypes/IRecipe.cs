#nullable enable
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.recipe")]
	public interface IRecipe : IPrototypeBase, INormalExpensiveMode<IRecipe, IRecipeData>
	{
		[PublicAPI] 
		[DefaultValue(null)]
		IconSpecification? Icon { get; }

		[PublicAPI]
		[DefaultValue(null)]
		IItemSubgroup? Subgroup { get; }

		[PublicAPI]
		[DefaultValue("crafting")]
		IRecipeCategory Category { get; }
	}
}