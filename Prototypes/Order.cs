using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	public class Order
	{
		[PublicAPI]
		public string Value { get; }

		public static implicit operator string(Order order)
		{
			return order.Value;
		}
		[PublicAPI]
		public Order(dynamic data)
		{
			Value = (string) data;
		}
	}
}