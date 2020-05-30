#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace FactorioModLoader.Prototypes
{
	[TypeConverter(typeof(ColorTypeConverter))]
	public class Color
	{
		public float R { get; }
		public float G { get; }
		public float B { get; }
		public float A { get; } = 1;
		public Color(dynamic data)
		{
			if (data is IDictionary<string, object> named)
			{
				if (named.TryGetValue("r", out var r))
					R = (float)(double)r;
				else
					R = 0;
				if (named.TryGetValue("g", out var g))
					G = (float)(double)g;
				else
					G = 0;
				if (named.TryGetValue("b", out var b))
					B = (float)(double)b;
				else
					B = 0;
				if (named.TryGetValue("a", out var a))
					A = (float)(double)a;
				else
					A = 0;
				return;
			}

			if (data is List<object> list)
			{
				if (list.Count > 0)
					R = (float)(double)list[0];
				if (list.Count > 1)
					G = (float)(double)list[1];
				if (list.Count > 2)
					B = (float)(double)list[2];
				if (list.Count > 3)
					A = (float)(double)list[3];
				return;
			}
			throw new ApplicationException("Unknown color format!");
		}

		public Color(float r, float g, float b, float a = 1)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}
	}
	public class ColorTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (!(value is string s))
				return base.ConvertFrom(context, culture, value);
			return new Color(Utf8Json.JsonSerializer.Deserialize<dynamic>(s));
		}
	}

}