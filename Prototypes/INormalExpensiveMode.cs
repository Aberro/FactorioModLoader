using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	public interface INormalExpensiveMode<T, out TData> where T : IPrototypeBase
	{
		[PublicAPI]
		[AlternatingData(".normal", ".", ".expensive")]
		TData Normal { get; }
		[PublicAPI]
		[AlternatingData(".normal", ".", ".expensive")]
		TData Expensive { get; }
	}
}
