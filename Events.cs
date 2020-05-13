using System;
using MoonSharp.Interpreter;

namespace FactorioModLoader
{
	public delegate void StageEventHandler(object sender, StageEventArgs args);
	public class StageEventArgs : EventArgs
	{
		[JetBrains.Annotations.NotNull]
		public Script ExecutionContext { get; }

		public StageEventArgs([JetBrains.Annotations.NotNull]Script executionContext)
		{
			ExecutionContext = executionContext;
		}
	}

	public delegate void ModuleFileEventHandler(object sender, ModuleFileEventArgs args);

	public class ModuleFileEventArgs : EventArgs
	{
		public IModule Module { get; }
		public string FileName { get; }

		public ModuleFileEventArgs(IModule mod, string file)
		{
			Module = mod;
			FileName = file;
		}
	}

}
