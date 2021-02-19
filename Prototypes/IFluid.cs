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
	}
}