using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace FactorioModLoader.PropertyTree
{
	public class ModSettingsReader
	{

		public static int CacheSize = 1024 * 4;

		private enum Op
		{
			Exit = 0,
			ReadVersion,
			ReadBool,
			ReadDouble,
			ReadString,
			ReadList,
			ReadDictionary,
			ReadProperty,
			FinishReadList,
			FinishReadDictionary,
			PushInt,
			Callback,
		}

		private ref struct FastStream
		{

			private static class FastStreamProcessor
			{
				public static Command[] Processor;
				static FastStreamProcessor()
				{
					Processor = new[]
					{
						(Command) null!,
						ReadVersion,
						ReadBool,
						ReadDouble,
						ReadString,
						ReadDictionary,
						ReadDictionary,
						ReadProperty,
						FinishReadDictionary,
						FinishReadDictionary,
						PushInt,
						Callback,
					};
				}

				[MethodImpl(MethodImplOptions.NoInlining)]
				static void ReadVersion(ref FastStream fs)
				{
					var slice = fs.Slice(8);
					var major = BitConverter.ToUInt16(slice.Slice(0, 2));
					var minor = BitConverter.ToUInt16(slice.Slice(2, 2));
					var build = BitConverter.ToUInt16(slice.Slice(4, 2));
					var revision = BitConverter.ToUInt16(slice.Slice(6, 2));
					fs.Push(new Version(major, minor, build, revision));
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				static private void ReadBool(ref FastStream fs)
				{
					fs.Push(fs.Slice(1)[0] == 1);
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				static private void ReadDouble(ref FastStream fs)
				{
					fs.Push(BitConverter.ToDouble(fs.Slice(sizeof(double))));
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				static void ReadString(ref FastStream fs)
				{
					var isEmpty = fs.Slice(1)[0] == 1;
					if (isEmpty)
					{
						fs.Push(String.Empty);
						return;
					}
					var length = (int)fs.Slice(1)[0];
					if (length == 255)
						length = BitConverter.ToInt32(fs.Slice(sizeof(int)));
					Span<char> str = stackalloc char[length];
					var slice = str;
					var diff = fs._ptr + length - fs._read;
					while (diff > 0)
					{
						length = diff;
						Utf8.ToUtf16(fs._span.Slice(fs._ptr), slice, out _, out var written);
						slice = slice.Slice(written);
						fs.HotSwap();
						diff = fs._ptr + length - fs._read;
					}

					{
						Utf8.ToUtf16(fs.Slice(length), slice, out _, out var written);
						fs.Push(new string(str));
					}
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				static void ReadDictionary(ref FastStream fs)
				{
					var length = (int)BitConverter.ToUInt32(fs.Slice(sizeof(uint)));
					fs.PushOp(Op.PushInt, (Op)length, Op.FinishReadDictionary);
					for (int i = 0; i < length; i++)
					{
						fs.PushOp(Op.ReadProperty);
						fs.PushOp(Op.ReadString);
					}
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				static void FinishReadDictionary(ref FastStream fs)
				{
					var length = (int)fs.Pop();
					if (length == 0)
					{
						fs.Push(new Dictionary<string, object>());
						return;
					}
					var firstObject = fs.Pop();
					var firstName = (string)fs.Pop();
					if (firstName == string.Empty)
					{
						var result = new object[length];
						result[0] = firstObject;
						for (int i = length - 1; i >= 1; i--)
						{
							result[i] = fs.Pop();
							fs.Pop();
						}
						fs.Push(result);
					}
					else
					{
						var result = new Dictionary<string, object>(length);
						result.Add(firstName, firstObject);
						for (int i = length - 1; i >= 1; i--)
						{
							var value = fs.Pop();
							var key = (string)fs.Pop();
							result.Add(key, value);
						}
						fs.Push(result);
					}

				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				static void ReadProperty(ref FastStream fs)
				{
					var slice = fs.Slice(2);
					var type = slice[0];
					//var anyType = slice[1];
					switch (type)
					{
						case 0:
							fs.Push(null!);
							break;
						case 1:
							fs.PushOp(Op.ReadBool);
							break;
						case 2:
							fs.PushOp(Op.ReadDouble);
							break;
						case 3:
							fs.PushOp(Op.ReadString);
							break;
						case 4:
							fs.PushOp(Op.ReadList);
							break;
						case 5:
							fs.PushOp(Op.ReadDictionary);
							break;
					}
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				static void PushInt(ref FastStream fs)
				{
					fs.Push((int)fs.PopOp());
				}
				[MethodImpl(MethodImplOptions.NoInlining)]
				static void Callback(ref FastStream fs)
				{
					var argsNum = (int)fs.Pop();
					object[] args = new object[argsNum];
					for (int i = 0; i < argsNum; i++)
						args[i] = fs.Pop();
					((CallbackDelegate)fs.Pop())(fs, args);
				}
			}
			public delegate void CallbackDelegate(FastStream fs, object[] args);
			public delegate void Command(ref FastStream fs);

			private Stream _stream;
			private Stack<object> _stack;
			private Span<byte> _span;
			private Span<byte> _backup;
			private Span<byte> _concat;
			private Span<Op> _opStack;
			private int _ptr;
			private int _opStackPtr;
			private int _read;
			private int _readBackup;
			private int _opCounter;


			public FastStream(Stream stream, Span<byte> span, Span<byte> backup, Span<byte> concatBuffer, Span<Op> stack)
			{
				_stream = stream;
				_span = span;
				_backup = backup;
				_concat = concatBuffer;
				_opStack = stack;
				_opStackPtr = 0;
				_opCounter = 0;
				_stack = new Stack<object>(64);
				_ptr = 0;
				_read = stream.Read(_span);
				_readBackup = _read == _span.Length ? stream.Read(_backup) : 0;
			}
			public ReadOnlySpan<byte> Slice(int length)
			{
				var residue = (_ptr + length) - _read;
				if (residue <= 0)
				{
					var result = _span.Slice(_ptr, length);
					_ptr += length;
					return result;
				}

				var part1 = _span.Slice(_ptr);
				part1.CopyTo(_concat);
				if (!HotSwap())
					throw new ApplicationException("End of file reached!");
				var part2 = _span.Slice(_ptr, residue);
				_ptr += residue;
				part2.CopyTo(_concat.Slice(length - residue));
				return _concat;
			}
			public void Push(object val) => _stack.Push(val);
			public object Pop() => _stack.Pop();
			public void PushOp(Op op) => _opStack[_opStackPtr++] = op;
			public void PushOp(params Op[] ops)
			{
				for (int i = ops.Length - 1; i >= 0; i--)
					_opStack[_opStackPtr++] = ops[i];
			}
			public Op PopOp() => _opStack[--_opStackPtr];

			public void Start()
			{
				var op = PopOp();
				while (op != Op.Exit)
				{
					_opCounter++;
					FastStreamProcessor.Processor[(int)op](ref this);
					op = PopOp();
				}
			}
			private bool HotSwap()
			{
				var tmp = _span;
				_span = _backup;
				_read = _readBackup;
				_backup = tmp;
				_ptr = 0;
				if (_readBackup != _span.Length) return false;
				_readBackup = _stream.Read(_backup);
				return true;
			}
		}
		public static object Read(Stream stream, Version factorioVersion)
		{
			Span<byte> span = stackalloc byte[CacheSize / 2];
			Span<byte> backup = stackalloc byte[CacheSize / 2];
			// size of longest primitive;
			Span<byte> concat = stackalloc byte[sizeof(decimal)];
			Span<Op> stack = stackalloc Op[2048];
			FastStream fs = new FastStream(stream, span, backup, concat, stack);
			// inverse order, so AssertStart is last
			ReadPropertyTree(ref fs);
			AssertStart(ref fs, factorioVersion);
			fs.Start();
			return fs.Pop();
		}
		private static void ReadPropertyTree(ref FastStream fs)
		{
			fs.PushOp(Op.ReadProperty, Op.Exit);
		}
		private static void AssertStart(ref FastStream fs, Version version)
		{
			fs.Push((FastStream.CallbackDelegate)((stream, args) =>
			{
				Debug.Assert(!((bool)args[0]));
				var version = (Version)args[1];
				Debug.Assert(version.Equals(version));
			}));
			fs.PushOp(Op.ReadVersion,
				Op.ReadBool,
				Op.PushInt,
				(Op)2,
				Op.Callback);
		}
	}
}
