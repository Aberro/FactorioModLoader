#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.technology")]
	[AccessorArgument("unit")]
	public interface ITechnology : IPrototypeBase, INormalExpensiveMode<ITechnology, ITechnologyData>
	{
		[PublicAPI]
		IconSpecification Icon { get; }
	}

}
