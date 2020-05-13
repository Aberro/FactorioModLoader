using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;

namespace FactorioModLoader
{
	internal class DataLoader
	{
		private readonly IProxyGenerator _generator;
		private readonly Dictionary<string, object> _repositories;

		public static DataLoader? Current { get; private set; }

		public DataLoader()
		{
			_generator = new ProxyGenerator();
			_repositories = new Dictionary<string, object>();
		}
		public IDictionary<string, T> LoadRepository<T>(string repositoryPath, dynamic data) where T : class
		{
			if(!(data is ExpandoObject eo))
				throw new ApplicationException("Expected expando object!");
			var result = eo.Where(x => x.Value != null).ToDictionary(x => x.Key, x => Proxy<T>(x.Value));
			_repositories.Add(repositoryPath, result);
			return result;
		}

		public object TryGetFromRepository(string repositoryPath, string key)
		{
			_repositories.TryGetValue(repositoryPath, out var repo);
			if (repo == null) throw new ArgumentException("Repository not found!", nameof(repositoryPath));
			var dic = (IDictionary) repo;
			return dic[key] ?? throw new ApplicationException();
		}

		public object ProxyValue(Type returnType, object value)
		{
			return ProxyValue(returnType, value, null);
		}
		private object ProxyValue(Type returnType, object value, PropertyInfo? property)
		{
			try
			{
				Current = this;
				// Interface
				if (returnType.IsInterface)
				{
					if (returnType.GetInterface("IEnumerable") != null || returnType.GetInterface("IList") != null)
					{
						dynamic result = typeof(List<>).MakeGenericType(returnType.GenericTypeArguments)
											 .GetConstructor(new Type[0])?.Invoke(new object?[0]) ??
										 throw new ApplicationException();
						if (!(value is IList<object> list))
						{
							if (property != null && property.GetCustomAttribute<AsSingular>() != null)
								result.Add(ProxyValue(returnType.GenericTypeArguments[0], value, property));
							else
								throw new ApplicationException("Array expected!");
						}
						else
						{
							foreach (var item in list)
							{
								var val = ProxyValue(returnType.GenericTypeArguments[0], item);
								result.Add((dynamic) val);
							}
						}

						return result;
					}

					if (!(value is string))
					{
						return _generator.CreateInterfaceProxyWithoutTarget(returnType,
							new Interceptor(this, value, returnType));
					}
				}

				if (value is string)
				{
					var repository = returnType.GetCustomAttribute<RepositoryAttribute>();
					if (repository != null)
					{
						var obj = TryGetFromRepository(repository.RepositoryPath, (string) value);
						if (obj != null)
							return obj;
					}
				}


				var builderAttr = returnType.GetCustomAttribute<BuilderAttribute>(false);
				if (builderAttr != null)
				{
					var builder = builderAttr.BuilderType == null ? returnType.GetMethod("Build") : builderAttr.BuilderType.GetMethod("Build");
					if (builder != null)
					{
						return builder.Invoke(null, new[] { value }) ?? throw new NullReferenceException("Build() should never return null values!");
					}
					else
						throw new ApplicationException("Builder's Build() method not found!");
				}

				if (returnType.IsAbstract)
				{
					return _generator.CreateClassProxy(returnType, new Interceptor(this, value, returnType));
				}

				var ctor = returnType.GetConstructor(new[] { typeof(DynamicMetaObject) });
				if (ctor != null)
				{
					return ctor.Invoke(new[] { value });
				}

				// presumably some system type
				if (returnType.GetInterface("IConvertible") != null && value is IConvertible convertible)
					return convertible.ToType(returnType, CultureInfo.InvariantCulture);
				if (value is ExpandoObject)
					throw new ApplicationException("Can't proxy value!");
				if(!returnType.IsInstanceOfType(value))
					throw new ApplicationException("Invalid value type!");
				return value;
			}
			catch
			{
				Current = null;
				throw;
			}
		}

		private T Proxy<T>(object data) where T : class
		{
			try
			{
				Current = this;
				return _generator.CreateInterfaceProxyWithoutTarget<T>(new Interceptor(this, data, typeof(T)));
			}
			catch
			{
				Current = null;
				throw;
			}
		}

		private class Interceptor : IInterceptor
		{
			private readonly DataLoader _loader;
			private readonly dynamic _data;
			private readonly Dictionary<MethodInfo, object?> _cache;
			public Interceptor(DataLoader loader, object data, Type proxiedType)
			{
				_loader = loader;

				_data = data;
				_cache = new Dictionary<MethodInfo, object?>();
			}

			public void Intercept(IInvocation invocation)
			{
				if (_cache.TryGetValue(invocation.Method, out var result))
				{
					invocation.ReturnValue = result;
					return;
				}

				var methodName = invocation.Method.Name;
				if (!methodName.StartsWith("get_"))
					throw new ApplicationException("Only property reading is supported!");
				var propertyName = methodName.Substring(4);
				var dic = (IDictionary<string, object>)_data;
				result = ResolveValue(propertyName, invocation.Method.ReturnType, invocation.Proxy, dic);
				_cache.Add(invocation.Method, result);
				invocation.ReturnValue = result;
			}

			private object? ResolveValue(string propertyName, Type returnType, object proxy, IDictionary<string, object> dic)
			{
				if (returnType.GetCustomAttribute<InitializeByContainer>() != null)
				{
					var ctor = returnType.GetConstructor(new[]
						{typeof(string), typeof(DynamicMetaObject)});
					if (ctor != null)
					{
						return ctor.Invoke(new[] { propertyName, _data });
					}
				}

				var proxyType = proxy.GetType();
				var baseType = proxyType.BaseType;
				PropertyInfo? property = baseType?.GetProperty(propertyName);
				if (property == null)
					foreach (var iface in proxyType.GetInterfaces())
					{
						property = iface.GetProperty(propertyName);
						if (property != null)
							break;
					}
				if (property == null)
					throw new ApplicationException($"Property {propertyName} not found!");
				baseType = property.DeclaringType ?? throw new ApplicationException();
				var accessorAttr = property.GetCustomAttribute<AccessorAttribute>();
				if (accessorAttr != null)
				{
					var accessorMethod = accessorAttr.AccessorType.GetMethod(propertyName);
					if (accessorMethod == null) throw new ApplicationException($"Accessor method {propertyName}() not found!");
					return accessorMethod.Invoke(null, new[] { proxy, _data });
				}

				var key = TryGetKey(property, propertyName, dic);
				if (key != null)
				{
					var result = _loader.ProxyValue(returnType, dic[key], property);
					if (result is ExpandoObject)
						throw new ApplicationException("Cant proxy value!");
					return result;
				}

				var attr = property.GetCustomAttribute<DefaultValueAttribute>();
				if (attr != null)
				{
					var result = attr.Value;
					if(result == null)
						throw new ApplicationException("Default value can't be null! (Leave property as nullable reference type instead)");
					if (result is IConvertible convertible)
						return convertible.ToType(returnType, CultureInfo.InvariantCulture);
					return result;
				}

				if (IsNullable(property.DeclaringType, property)) return null;
				throw new ApplicationException($"Mandatory property {baseType.FullName}.{propertyName} is undefined!");
			}

			private string? TryGetKey(PropertyInfo property, string propertyName, IDictionary<string, object> dic)
			{
				List<string> aliases = new List<string>(3)
				{
					propertyName, FormatPropertyName(propertyName, '-'), FormatPropertyName(propertyName, '_')
				};
				var aliasAttr = property.GetCustomAttribute<AliasAttribute>(true);
				var singularAttr = property.GetCustomAttribute<AsSingular>(true);
				if (aliasAttr != null)
					aliases.AddRange(aliasAttr.Aliases);
				if(singularAttr != null)
					aliases.Add(singularAttr.SingularName);
				string? key = null;
				foreach (var name in aliases)
				{
					key = dic.Keys.FirstOrDefault(x => x.Equals(name, StringComparison.InvariantCultureIgnoreCase));
					if (key != null)
						break;
				}

				return key;
			}

			private string FormatPropertyName(string propertyName, char ch)
			{
				var chars = new char[propertyName.Length + propertyName.Count(char.IsUpper) - 1];
				for (int i = 0, j = 0; i < propertyName.Length; i++, j++)
				{
					var c = propertyName[i];
					if (char.IsUpper(c) && i != 0)
						chars[j++] = ch;
					chars[j] = c;
				}
				return new string(chars);
			}
			private static bool IsNullable(Type enclosingType, PropertyInfo property)
			{
				if (!enclosingType.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Contains(property))
					throw new ArgumentException("enclosingType must be the type which defines property");

				var nullable = property.CustomAttributes
					.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
				if (nullable != null && nullable.ConstructorArguments.Count == 1)
				{
					var attributeArgument = nullable.ConstructorArguments[0];
					if (attributeArgument.ArgumentType == typeof(byte[]))
					{
						var args = (ReadOnlyCollection<CustomAttributeTypedArgument>?)attributeArgument.Value;
						if (args != null && args.Count > 0 && args[0].ArgumentType == typeof(byte))
						{
							var val = args[0].Value;
							return val != null ? (byte)val == 2 : throw new NullReferenceException();
						}
					}
					else if (attributeArgument.ArgumentType == typeof(byte))
					{
						var val = attributeArgument.Value;
						return val != null ? (byte)val == 2 : throw new NullReferenceException();
					}
				}

				var context = enclosingType.CustomAttributes
					.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
				if (context != null &&
				    context.ConstructorArguments.Count == 1 &&
				    context.ConstructorArguments[0].ArgumentType == typeof(byte))
				{
					var val = context.ConstructorArguments[0].Value;
					return val != null ? (byte)val == 2 : throw new NullReferenceException();
				}

				// Couldn't find a suitable attribute
				return false;
			}
		}

	}
}
