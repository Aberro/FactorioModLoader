using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace FactorioModLoader.Prototypes
{
	[TypeConverter(typeof(VectorTypeConverter))]
	public class Vector
	{
		public static readonly Vector Zero = new Vector(0, 0);
		public float X { get; }
		public float Y { get; }
		public Vector(dynamic data)
		{
			if (data is IDictionary<string, object> named)
			{
				if (named.TryGetValue("x", out var x))
					X = (float)(double)x;
				else
					X = 0;
				if (named.TryGetValue("y", out var y))
					Y = (float)(double)y;
				else
					Y = 0;
			}

			if (data is List<object> list)
			{
				if (list.Count > 0)
					X = (float)(double)list[0];
				if (list.Count > 1)
					Y = (float)(double)list[1];
			}
			else
				throw new ApplicationException("Unknown vector format!");
		}

		public Vector(float x, float y)
		{
			X = x;
			Y = y;
		}
	}
	public class VectorTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (!(value is string s))
				return base.ConvertFrom(context, culture, value);
			return new Vector(Utf8Json.JsonSerializer.Deserialize<dynamic>(s));
		}
	}
}