using System.Diagnostics.CodeAnalysis;
using Castle.DynamicProxy;
using JetBrains.Annotations;

namespace FactorioRecipeCalculator
{
	public static class Builder
	{
		[System.Diagnostics.CodeAnalysis.NotNull]
		private static readonly IProxyGenerator Generator = new ProxyGenerator();
		class Interceptor : IInterceptor
		{
			private string _name;
			private dynamic _data;
			public void Intercept(IInvocation invocation)
			{
				
			}
			public Interceptor(string name, dynamic data)
			{
				_name = name;
				_data = data;
			}
		}

		static Builder()
		{
		}
		public static T Build<T>(string name, dynamic data) where T : class, IPrototype
		{
			return Generator.CreateInterfaceProxyWithoutTarget<T>(new Interceptor(name, data));
		}
	}
}
