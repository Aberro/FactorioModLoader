#nullable enable
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.recipe")]
	public interface IRecipe : IPrototypeBase
	{
		[Accessor(typeof(RecipeAccessor))]
		IRecipeData Normal { get; }
		[Accessor(typeof(RecipeAccessor))]
		IRecipeData Expensive { get; }
	}
}