#nullable enable
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.fluid")]
	public interface IFluid : IPrototypeBase
	{
		[PublicAPI]
		IconSpecification Icon { get; }
	}
}