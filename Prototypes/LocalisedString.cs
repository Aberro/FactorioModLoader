#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	public class LocalisedString
	{
		[PublicAPI]
		public IList<LocalisedString> Parameters { get; }
		[PublicAPI]
		public string Key { get; }

		[PublicAPI]
		public LocalisedString(dynamic data)
		{
			if (data is string str)
			{
				Key = str;
				Parameters = Array.Empty<LocalisedString>();
			}
			else
			{
				Key = (string) data[0];
				Parameters = ((IEnumerable<dynamic>)data).Skip(1).Select(x => new LocalisedString(x)).ToArray();
			}
		}
	}
}