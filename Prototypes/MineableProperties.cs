
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	//[InitializeByTargetType]
	public abstract class MineableProperties
	{
		[PublicAPI]
		public abstract double MiningTime { get; }

		[PublicAPI]
		[AsSingular("result")]
		public abstract IEnumerable<IProduct> Results { get; }

		[PublicAPI]
		[DefaultValue(0)]
		public abstract double FluidAmount { get; }

		[PublicAPI]
		[DefaultValue(null)]
		public abstract IFluid? RequiredFluid { get; }

		[PublicAPI]
		[DefaultValue(1)]
		public abstract ushort Count { get; }
		//public MineableProperties(dynamic data)
		//{

		//}
	}
}
