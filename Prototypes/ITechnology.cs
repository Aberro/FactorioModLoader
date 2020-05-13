using System.Collections.Generic;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.technology")]
	public interface ITechnology : IPrototypeBase
	{
		[PublicAPI]
		IconSpecification Icon { get; }
		IEnumerable<ITechnology>? Prerequisites { get; }
		IEnumerable<Modifier>? Effects { get; }
	}

}
