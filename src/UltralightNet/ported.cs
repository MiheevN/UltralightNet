using System.Runtime.CompilerServices;

#if !NET6_0_OR_GREATER
namespace System.Runtime.InteropServices
{
	internal static unsafe class NativeMemory
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void* Alloc(nuint size) => (void*)Marshal.AllocHGlobal(new IntPtr((void*)size));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Free(void* memory) => Marshal.FreeHGlobal((IntPtr)memory);
	}
}
#endif
#if !(NETCOREAPP1_0_OR_GREATER || NET46_OR_GREATER || NETSTANDARD1_3_OR_GREATER)
namespace System
{
	internal static unsafe class Buffer
	{
		public static void MemoryCopy(byte* to, byte* from, nuint _, nuint length){
			if(length < int.MaxValue) new ReadOnlySpan<byte>(from, (int)length).CopyTo(new Span<byte>(to, (int)length));
			else for(nuint i = 0; i < length; i++) from[i] = to[i];
		}
	}
}
#endif
#if !(NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1)
namespace System.Diagnostics.CodeAnalysis
{
	internal sealed class MaybeNullWhenAttribute : Attribute
	{
		public MaybeNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}

		public bool ReturnValue { get; }
	}
}
#endif
