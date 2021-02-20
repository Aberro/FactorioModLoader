#nullable enable
using System;
using System.Collections.Generic;
using System.Dynamic;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[InitializeByTargetType(nameof(TryCreate))]
	[PublicAPI]
	public class IconSpecification
	{
		public IList<IIconData> Icons { get; }
		public static IconSpecification? TryCreate(string propertyName, dynamic container)
		{
			if (container is IDictionary<string, object> dic)
			{
				if (dic.TryGetValue("icon", out var icon) && icon != null)
				{
					if (dic.TryGetValue("icon_size", out _))
					{
						var result = new IconSpecification(propertyName, container);
						return result;
					}
				}
				else if (dic.TryGetValue("icons", out var icons) && icons != null)
					return new IconSpecification(propertyName, container);
			}
			return null;
		}
		[PublicAPI]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "api reasons")]
		public IconSpecification([UsedImplicitly]string propertyName, dynamic container)
		{
			if (container is IDictionary<string, object> dic)
			{
				if (dic.TryGetValue("icon", out var icon) && icon != null)
				{
					if (dic.TryGetValue("icon_size", out var iconSize))
					{
						dynamic iconData = new ExpandoObject();
						iconData.icon = icon;
						iconData.icon_size = iconSize;
						Icons = new List<IIconData>(new[]
						{
							(IIconData) (DataLoader.Current?.ProxyValue(typeof(IIconData), iconData) ??
							             throw new ApplicationException())
						});
					}
					else
						throw new ApplicationException("Both 'icon' and 'icon_size' should be specified!");
				}
				else if (dic.TryGetValue("icons", out var icons) && icons != null)
				{
					dic.TryGetValue("icon_size", out var globalIconSize);
					List<IIconData> result = new List<IIconData>();
					foreach (var data in (dynamic) icons)
					{
						if(data == null)
							continue;
						var expando = (IDictionary<string, object>) data;
						if(!expando.ContainsKey("icon_size") && globalIconSize != null)
							expando.Add("icon_size", globalIconSize);
						var item = DataLoader.Current?.ProxyValue(typeof(IIconData), data);
						result.Add(item);
					}
					Icons = result;
				}
				else
					throw new ApplicationException("No icon properties found!");
			}
			else
			{
				throw new ApplicationException("Expected ExpandoObject!");
			}
		}
	}
}