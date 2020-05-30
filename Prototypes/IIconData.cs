#nullable enable
using System.ComponentModel;

namespace FactorioModLoader.Prototypes
{
	public interface IIconData
	{
		public string Icon { get; }
		public short IconSize { get; }
		[DefaultValue(typeof(Color), "[0,0,0]")]
		public Color Tint { get; }
		[DefaultValue(1)]
		public double Scale { get; }
		[DefaultValue(typeof(Vector), "[0,0]")]
		public Vector Shift { get; }
		[DefaultValue(0)]
		public byte Mipmaps { get; }
	}
}