using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	[Repository("data.raw.tool")]
	public interface ITool : IItem
	{
	}
	[PublicAPI]
	[Repository("data.raw.armor")]
	public interface IArmor : ITool
	{
	}
	[PublicAPI]
	[Repository("data.raw.gun")]
	public interface IGun : ITool { }
	[Repository("data.raw.ammo")]
	public interface IAmmoItem : ITool { }
	[Repository("data.raw.capsule")]
	public interface ICapsule : ITool { }
	[Repository("data.raw.module")]
	public interface IModule : ITool { }
	[Repository("data.raw.mining-tool")]
	public interface IMiningTool : ITool { }
	[Repository("data.raw.repair-tool")]
	public interface IRepairTool : ITool { }
}
