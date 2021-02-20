#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	public class LocalizedString
	{
		[PublicAPI]
		public IList<LocalizedString> Parameters { get; }
		[PublicAPI]
		public string Key { get; }

		[PublicAPI]
		public LocalizedString(dynamic data)
		{
			if (data is string str)
			{
				Key = str;
				Parameters = Array.Empty<LocalizedString>();
			}
			else
			{
				Key = (string) data[0];
				Parameters = ((IEnumerable<dynamic>)data).Skip(1).Select(x => new LocalizedString(x)).ToArray();
			}
		}
	}
}