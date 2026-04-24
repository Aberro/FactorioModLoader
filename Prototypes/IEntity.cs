using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[Repository("data.raw.entity")]
	[Repository("data.raw.crafting-machine")]
	public interface IEntity : IPrototypeBase
	{
		[PublicAPI]
		[DefaultValue(null)]
		MineableProperties? Minable { get; }

	}
}
