#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.technology")]
	public interface ITechnology : IPrototypeBase
	{
		[PublicAPI]
		IconSpecification Icon { get; }
		[Accessor(typeof(TechnologyAccessor))]
		public ITechnologyData Normal { get; }
		[Accessor(typeof(TechnologyAccessor))]
		public ITechnologyData Expensive { get; }
	}

}
