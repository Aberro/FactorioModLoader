using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	public interface IModuleSpecification
	{
		[PublicAPI]
		[DefaultValue(0)]
		ushort ModuleSlots { get; }
	}
}
