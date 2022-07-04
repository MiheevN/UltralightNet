using System;
using System.Runtime.CompilerServices;

namespace UltralightNet.LowStuff;

public unsafe interface INativeContainerInterface<TSelf> where TSelf : INativeContainerInterface<TSelf>
{
#if NET7_0_OR_GREATER
	public static abstract TSelf FromHandle(Handle<TSelf> ptr, bool dispose);
#endif
}
public struct Handle<T>
{
	private nuint _value;

	private unsafe Handle(void* _value)
	{
		this._value = (nuint)_value;
	}

	public override bool Equals(object? other) => other is Handle<T> h && _value == h._value;
	public override int GetHashCode() => (int)_value;

	public static bool operator ==(Handle<T> left, Handle<T> right) => left._value == right._value;
	public static bool operator !=(Handle<T> left, Handle<T> right) => left._value != right._value;

	public static explicit operator Handle<T>(IntPtr ptr) => Unsafe.As<IntPtr, Handle<T>>(ref ptr);
	public static unsafe explicit operator Handle<T>(void* ptr) => new(ptr);
	public static unsafe explicit operator void*(Handle<T> handle) => (void*)handle._value;
	public static unsafe explicit operator IntPtr(Handle<T> handle) => Unsafe.As<nuint, IntPtr>(ref handle._value);
}
public unsafe abstract class INativeContainer<TSelf> : IDisposable where TSelf : INativeContainer<TSelf>, INativeContainerInterface<TSelf>, IEquatable<TSelf>
{
	private Handle<TSelf> _ptr;
	internal virtual Handle<TSelf> Handle { get => !IsDisposed ? _ptr : throw new ObjectDisposedException(nameof(TSelf)); init => _ptr = value; }
	public bool IsDisposed { get; protected set; }
	protected bool Owns { get; init; } = true;

	public abstract void Dispose();
	~INativeContainer() => Dispose(); // it does work (tested on MODiX)

	public override bool Equals(object? other) => other is TSelf container && Equals(container);
	public override int GetHashCode() => !IsDisposed ? Handle.GetHashCode() : 0;
}
