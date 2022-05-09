using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using UltralightNet.AppCore;

namespace UltralightNet.OpenGL.TestApp;

public static class Program
{
	const string VertexShaderSource = @"
#version 420 core
layout (location = 0) in vec4 vPos;
layout (location = 0) out vec2 uv;
void main()
{
	uv = vec2(vPos.z, vPos.w);
    gl_Position = vec4(vPos.x, -vPos.y, 0, 1.0);
}
";

	const string FragmentShaderSource = @"
#version 420 core
layout (location = 0) in vec2 uv;
uniform sampler2D rt;
out vec4 FragColor;

void main()
{
	FragColor = texture(rt, uv);
}
";

	static float[] VertexBuffer = new float[]{
		-1f, 1f, 0f, 1f,
		1f, 1f, 1f, 1f,
		1f,-1f, 1f, 0f,
		-1f,-1f, 0f, 0f
	};

	static uint[] IndexBuffer = new uint[]
	{
		0,1,2,
		2,3,0
	};

	static IWindow window;
	static ICursor? cursor;
	static GL gl;

	static uint vao = 0, vbo = 0, ebo = 0;
	static uint quadProgram = 0;

	static Renderer renderer;
	static View view;

	static OpenGLGPUDriver gpuDriver;

	public static void Main()
	{
		AppContext.SetSwitch("Switch.System.Reflection.Assembly.SimulatedLocationInBaseDirectory", true);
		AppCoreMethods.ulEnablePlatformFontLoader();
		AppCoreMethods.ulEnablePlatformFileSystem("./");
		AppCoreMethods.ulEnableDefaultLogger("./log123as.txt");

		window = Window.Create(WindowOptions.Default with
		{
			Size = new Vector2D<int>(512, 512),
			API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, 0/*ContextFlags.ForwardCompatible*/, new APIVersion(4, 5)),
			Samples = 1,
			FramesPerSecond = 60,
			UpdatesPerSecond = 60,
			VSync = true
		});

		window.Load += OnLoad;
		window.Update += OnUpdate;
		window.Render += OnRender;
		window.FramebufferResize += s =>
		{
			gl.Viewport(s);
			view.Resize((uint)s.X, (uint)s.Y);
			System.Threading.Thread.Sleep(1000 / 240);
		};

		window.Run();
	}

	// [UnmanagedCallersOnly(CallConvs = new Type[]{typeof(CallConvCdecl)})]
	private static unsafe void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint chars, nint userData){
		Console.WriteLine(Marshal.PtrToStringUni((IntPtr)chars, (int)length));
	}
	private static void Check([CallerLineNumber] int line = default)
	{
#if DEBUG
		var error = gl.GetError();
		if (error is not 0) Console.WriteLine($"{line}: {error}");
#endif
	}

	static unsafe void OnLoad()
	{
		IInputContext input = window.CreateInput();
		for (int i = 0; i < input.Keyboards.Count; i++)
		{
			//input.Keyboards[i].KeyDown += KeyDown;
		}
		input.Mice[0].Scroll += OnScroll;
		input.Mice[0].MouseDown += OnMouseDown;
		input.Mice[0].MouseUp += OnMouseUp;
		input.Mice[0].MouseMove += OnMouseMove;
		cursor = input.Mice[0].Cursor;

		//Getting the opengl api for drawing to the screen.
		gl = GL.GetApi(window);

		gl.DebugMessageCallback(DebugCallback, null);

		Check();
		//gl.Enable(GLEnum.FramebufferSrgb);

		gl.CullFace(GLEnum.Back);
		gl.Disable(GLEnum.CullFace);
		gl.FrontFace(GLEnum.Ccw);
		gl.Disable(EnableCap.DepthTest);
		gl.DepthFunc(DepthFunction.Never);
		//gl.Enable(GLEnum.Multisample);
		//gl.Disable(GLEnum.StencilTest);

		//Creating a vertex array.
		vao = gl.GenVertexArray();
		gl.BindVertexArray(vao);

		//Initializing a vertex buffer that holds the vertex data.
		vbo = gl.GenBuffer(); //Creating the buffer.
		gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo); //Binding the buffer.
		fixed (void* v = &VertexBuffer[0])
		{
			gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(VertexBuffer.Length * sizeof(float)), v, BufferUsageARB.StaticDraw); //Setting buffer data.
		}

		//Initializing a element buffer that holds the index data.
		ebo = gl.GenBuffer(); //Creating the buffer.
		gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo); //Binding the buffer.
		fixed (void* i = &IndexBuffer[0])
		{
			gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(IndexBuffer.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw); //Setting buffer data.
		}

		//Creating a vertex shader.
		uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
		gl.ShaderSource(vertexShader, VertexShaderSource);
		gl.CompileShader(vertexShader);

		//Checking the shader for compilation errors.
		string infoLog = gl.GetShaderInfoLog(vertexShader);
		if (!string.IsNullOrWhiteSpace(infoLog))
		{
			Console.WriteLine($"Error compiling vertex shader {infoLog}");
		}

		//Creating a fragment shader.
		uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
		gl.ShaderSource(fragmentShader, FragmentShaderSource);
		gl.CompileShader(fragmentShader);

		//Checking the shader for compilation errors.
		infoLog = gl.GetShaderInfoLog(fragmentShader);
		if (!string.IsNullOrWhiteSpace(infoLog))
		{
			Console.WriteLine($"Error compiling fragment shader {infoLog}");
		}

		//Combining the shaders under one shader program.
		quadProgram = gl.CreateProgram();
		gl.AttachShader(quadProgram, vertexShader);
		gl.AttachShader(quadProgram, fragmentShader);
		gl.BindAttribLocation(quadProgram, 0, "vPos");
		gl.LinkProgram(quadProgram);

		//Checking the linking for errors.
		gl.GetProgram(quadProgram, GLEnum.LinkStatus, out var status);
		if (status == 0)
		{
			Console.WriteLine($"Error linking shader {gl.GetProgramInfoLog(quadProgram)}");
		}

		//Delete the no longer useful individual shaders;
		gl.DetachShader(quadProgram, vertexShader);
		gl.DetachShader(quadProgram, fragmentShader);
		gl.DeleteShader(vertexShader);
		gl.DeleteShader(fragmentShader);

		//Tell opengl how to give the data to the shaders.

		gl.UseProgram(quadProgram);
		gl.Uniform1(gl.GetUniformLocation(quadProgram, "rt"), 0);

		gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), null);
		gl.EnableVertexAttribArray(0);

		window.SwapBuffers();

		gpuDriver = new(gl, 16);

		gpuDriver.Check();

		window.SwapBuffers();

		ULPlatform.GPUDriver = gpuDriver.GetGPUDriver();

		renderer = ULPlatform.CreateRenderer(new ULConfig { FaceWinding = ULFaceWinding.Clockwise, ForceRepaint = true, MaxUpdateTime = (double)1/(double)(60+10) });

		view = renderer.CreateView(512, 512, new ULViewConfig { IsAccelerated = true, IsTransparent = false });

		view.URL = "https://vk.com/supinepandora43";
		//view.URL = "https://twitter.com/@supinepandora43";
		//view.URL = "https://youtube.com";
		//view.HTML = "<html><body><p>123</p></body></html>";
		bool loaded = false;

		view.OnFinishLoading += (_, _, _) => loaded = true;

		while (!loaded)
		{
			renderer.Update();
			System.Threading.Thread.Sleep(10);
		}

		view.OnChangeCursor += (ulcursor) =>
		{
			cursor!.StandardCursor = ulcursor switch
			{
				ULCursor.Cross => StandardCursor.Crosshair,
				ULCursor.Hand => StandardCursor.Hand,
				ULCursor.IBeam => StandardCursor.IBeam,
				ULCursor.EastWestResize => StandardCursor.HResize,
				ULCursor.NorthSouthResize => StandardCursor.VResize,
				_ => StandardCursor.Default
			};
		};

		window.SwapBuffers();

		renderer.Render();

		window.SwapBuffers();
	}

	static unsafe void OnRender(double obj)
	{
		renderer.Update();
		renderer.Render();

		var renderBuffer = gpuDriver.renderBuffers[view.RenderTarget.RenderBufferId];
		var textureEntry = renderBuffer.textureEntry;

		// redraw only when it has changed
		if (renderBuffer.dirty)
		{
			gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, textureEntry.multisampledFramebuffer);
			gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, textureEntry.framebuffer);
			gl.BlitFramebuffer(0,0, (int)textureEntry.width, (int)textureEntry.height, 0,0, (int)textureEntry.width, (int)textureEntry.height, ClearBufferMask.ColorBufferBit, GLEnum.Linear);

			//gl.BlitNamedFramebuffer(textureEntry.multisampledFramebuffer, textureEntry.framebuffer, 0, 0, (int)textureEntry.width, (int)textureEntry.height, 0, 0, (int)textureEntry.width, (int)textureEntry.height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
			gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			//window.ClearContext();
			gl.ClearColor(0,1,0,0);
			gl.Clear((uint)(ClearBufferMask.ColorBufferBit));
			gl.Disable(GLEnum.DepthTest);
			gl.Disable(EnableCap.Blend);


			gl.BindVertexArray(vao);
			gl.UseProgram(quadProgram);
			gl.BindTextureUnit(0, textureEntry.textureId);
			gl.DrawElements(PrimitiveType.Triangles, (uint)IndexBuffer.Length, DrawElementsType.UnsignedInt, null);
			// renderBuffer.dirty = false;
			// window.FramesPerSecond = 0;
		}
		// else
			// lower fps
			// window.FramesPerSecond = 300;
	}

	static void OnUpdate(double obj)
	{
		//renderer.Update();
	}

	static void OnScroll(IMouse _, ScrollWheel scroll)
	{
		view.FireScrollEvent(new ULScrollEvent { Type = ULScrollEventType.ByPixel, DeltaX = (int)scroll.X * 100, DeltaY = (int)scroll.Y * 100 });
	}

	static void OnMouseDown(IMouse mouse, MouseButton button)
	{
		view.FireMouseEvent(new() { Type = ULMouseEventType.MouseDown, Button = button is MouseButton.Left ? ULMouseEventButton.Left : (button is MouseButton.Right ? ULMouseEventButton.Right : ULMouseEventButton.Middle), X = (int)mouse.Position.X, Y = (int)mouse.Position.Y });
	}

	static void OnMouseUp(IMouse mouse, MouseButton button)
	{
		view.FireMouseEvent(new() { Type = ULMouseEventType.MouseUp, Button = button is MouseButton.Left ? ULMouseEventButton.Left : (button is MouseButton.Right ? ULMouseEventButton.Right : ULMouseEventButton.Middle), X = (int)mouse.Position.X, Y = (int)mouse.Position.Y });
	}
	static void OnMouseMove(IMouse mouse, Vector2 position)
	{
		view.FireMouseEvent(new() { Type = ULMouseEventType.MouseMoved, X = (int)position.X, Y = (int)position.Y });
	}
}
