using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[Repository("data.raw.item-subgroup")]
	public interface IItemSubgroup : IPrototypeBase
	{
		[PublicAPI]
		IItemGroup Group { get; }
	}
}
