#nullable enable
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.item")]
	[Repository("data.raw.tool")]
	[Repository("data.raw.armor")]
	[Repository("data.raw.gun")]
	[Repository("data.raw.ammo")]
	[Repository("data.raw.capsule")]
	[Repository("data.raw.module")]
	[Repository("data.raw.mining-tool")]
	[Repository("data.raw.repair-tool")]
	[Repository("data.raw.rail-planner")]
	[Repository("data.raw.item-with-entity-data")]
	[Repository("data.raw.selection-tool")]
	[Repository("data.raw.spidertron-remote")]
	public interface IItem : IPrototypeBase
	{
		[PublicAPI]
		IconSpecification Icon { get; }

		[PublicAPI]
		[DefaultValue("other")]
		IItemSubgroup Subgroup { get; }

		[PublicAPI]
		[DefaultValue(null)]
		IEntity? PlaceResult { get; }

		[PublicAPI]
		[DefaultValue(null)]
		IEquipment? PlacedAsEquipmentResult { get; }
	}
}