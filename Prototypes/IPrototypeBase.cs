#nullable enable
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	public interface IPrototypeBase
	{
		/// <summary>
		/// Unique textual identification of the prototype.
		/// </summary>
		/// <remarks>
		/// May not contain ., may not exceed a length of 200 characters.
		/// </remarks>
		[PublicAPI]
		string Name { get; }
		/// <summary>
		/// Specification of the type of the prototype.
		/// </summary>
		[PublicAPI]
		string Type { get; }
		/// <summary>
		/// Overwrites the description set in the locale file. The description is usually shown in the tooltip of the prototype.
		/// </summary>
		[PublicAPI]
		LocalizedString? LocalizedDescription { get; }
		/// <summary>
		/// Overwrites the name set in the locale file. Can be used to easily set a procedurally-generated name because the LocalisedString format allows to insert parameters into the name directly from the Lua script.
		/// </summary>
		[PublicAPI]
		LocalizedString? LocalizedName { get; }
		/// <summary>
		/// Used to order items in inventory, recipes and GUI's.
		/// </summary>
		/// <remarks>May not exceed a length of 200 characters.</remarks>
		[PublicAPI]
		Order? Order { get; }
	}
}
