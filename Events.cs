using System;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace FactorioModLoader
{
	[PublicAPI]
	public delegate void StageEventHandler(object sender, StageEventArgs args);
	[PublicAPI]
	public class StageEventArgs : EventArgs
	{
		[PublicAPI]
		public Script ExecutionContext { get; }

		public StageEventArgs(Script executionContext)
		{
			ExecutionContext = executionContext;
		}
	}

	[PublicAPI]
	public delegate void ModuleFileEventHandler(object sender, ModuleFileEventArgs args);
	[PublicAPI]
	public class ModuleFileEventArgs : EventArgs
	{
		[PublicAPI]
		public IModule Module { get; }
		[PublicAPI]
		public string FileName { get; }

		public ModuleFileEventArgs(IModule mod, string file)
		{
			Module = mod;
			FileName = file;
		}
	}

	[PublicAPI]
	public delegate void ModuleEventHandler(object sender, ModuleEventArgs args);
	[PublicAPI]
	public class ModuleEventArgs : EventArgs
	{
		[PublicAPI]
		public IModule Module { get; }

		public ModuleEventArgs(IModule mod)
		{
			Module = mod;
		}
	}

	[PublicAPI]
	public delegate void LoadingErrorEventHandler(object sender, LoadingErrorEventArgs args);
	[PublicAPI]
	public class LoadingErrorEventArgs : EventArgs
	{
		[PublicAPI]
		public string? Error { get; }

		public LoadingErrorEventArgs(string error)
		{
			Error = error;
		}
	}
}
