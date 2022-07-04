using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UltralightNet.LowStuff;

namespace UltralightNet;

public static unsafe partial class Methods
{
	[DllImport(LibUltralight)]
	public static extern Handle<Renderer> ulCreateRenderer(_ULConfig* config);

	// INTEROPTODO: NATIVEMARSHALLING
	//[GeneratedDllImport(LibUltralight)]
	public static Handle<Renderer> ulCreateRenderer(in ULConfig config)
	{
		_ULConfig nativeConfig = new(config);
		var ret = ulCreateRenderer(&nativeConfig);
		nativeConfig.Dispose();
		return ret;
	}

	/// <summary>Destroy the renderer.</summary>
	[DllImport(LibUltralight)]
	public static extern void ulDestroyRenderer(Handle<Renderer> renderer);

	/// <summary>Update timers and dispatch internal callbacks (JavaScript and network).</summary>
	[DllImport(LibUltralight)]
	public static extern void ulUpdate(Handle<Renderer> renderer);

	/// <summary>Render all active Views.</summary>
	[DllImport(LibUltralight)]
	public static extern void ulRender(Handle<Renderer> renderer);

	/// <summary>Attempt to release as much memory as possible. Don't call this from any callbacks or driver code.</summary>
	[DllImport(LibUltralight)]
	public static extern void ulPurgeMemory(Handle<Renderer> renderer);

	[DllImport(LibUltralight)]
	public static extern void ulLogMemoryUsage(Handle<Renderer> renderer);
}

public class Renderer : INativeContainer<Renderer>, INativeContainerInterface<Renderer>, IEquatable<Renderer>
{
	private readonly Handle<Renderer> _handle;
	internal override Handle<Renderer> Handle
	{
		get
		{
			static void Throw() => throw new ObjectDisposedException(nameof(Renderer));
			if (IsDisposed) Throw();
			ULPlatform.CheckThread();
			return _handle;
		}
		init => _handle = value;
	}

	private Renderer() { }

	public View CreateView(uint width, uint height) => CreateView(width, height, new ULViewConfig());
	public View CreateView(uint width, uint height, ULViewConfig viewConfig) => CreateView(width, height, viewConfig, DefaultSession);
	public View CreateView(uint width, uint height, ULViewConfig viewConfig, Session session) => CreateView(width, height, viewConfig, session, true);

	public View CreateView(uint width, uint height, ULViewConfig viewConfig, Session session, bool dispose)
	{
		if (Owns && ULPlatform.ErrorGPUDriverNotSet && viewConfig.IsAccelerated && !ULPlatform.gpudriverSet)
		{
			throw new Exception("No ULPlatform.GPUDriver set, but ULViewConfig.IsAccelerated==true. (Disable error by setting ULPlatform.ErrorGPUDriverNotSet to false.)");
		}
		View view = new(Methods.ulCreateView(Handle, width, height, viewConfig, session.Handle), dispose);
		GC.KeepAlive(this);
		view.Renderer = this;
		return view;
	}
	/// <summary>Create a Session to store local data in (such as cookies, local storage, application cache, indexed db, etc).</summary>
	/// <remarks>A default, persistent Session is already created for you. You only need to call this if you want to create private, in-memory session or use a separate session for each View.</remarks>
	/// <param name="is_persistent">Whether or not to store the session on disk.<br/>Persistent sessions will be written to the path set in <see cref="ULConfig.CachePath"/></param>
	/// <param name="name">A unique name for this session, this will be used to generate a unique disk path for persistent sessions.</param>
	public Session CreateSession(bool isPersistent, string name)
	{
		var returnValue = Session.FromHandle(Methods.ulCreateSession((Handle<Renderer>)Handle, isPersistent, name), true);
		GC.KeepAlive(this);
		return returnValue;
	}
	public Session DefaultSession
	{
		get
		{
			Session returnValue = Session.FromHandle(Methods.ulDefaultSession((Handle<Renderer>)Handle), false);
			GC.KeepAlive(this);
			return returnValue;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Update() { Methods.ulUpdate(Handle); GC.KeepAlive(this); }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Render() { Methods.ulRender(Handle); GC.KeepAlive(this); }
	public void PurgeMemory() { Methods.ulPurgeMemory(Handle); GC.KeepAlive(this); }
	public void LogMemoryUsage() { Methods.ulLogMemoryUsage(Handle); GC.KeepAlive(this); }

	public override void Dispose()
	{
		if (IsDisposed || !Owns) return;
		Methods.ulDestroyRenderer(Handle);
		ULPlatform.thread = null;

		IsDisposed = true;
		GC.SuppressFinalize(this);
	}

	public static Renderer FromHandle(Handle<Renderer> handle, bool dispose) => new() { Handle = handle, Owns = dispose };

	public bool Equals(Renderer? other)
	{
		if (other is null) return false;
		if (other.IsDisposed) return IsDisposed;
		return Handle == other.Handle;
	}
}
