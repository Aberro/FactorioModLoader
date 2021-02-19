using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[Repository("data.raw.item-group")]
	public interface IItemGroup : IPrototypeBase
	{
		[PublicAPI]
		IconSpecification Icon { get; }

		[PublicAPI]
		[DefaultValue(null)]
		string? OrderInRecipe { get; }
	}
}