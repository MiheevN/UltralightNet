using Supine.Unstride;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using UltralightNet.AppCore;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace UltralightNet.Veldrid.TestApp
{
	class Program
	{
		private const GraphicsBackend BACKEND = GraphicsBackend.Direct3D11;

		private const uint Width = 1024;
		private const uint Height = 512;

		[DllImport("Ultralight", EntryPoint = "?GetKeyIdentifierFromVirtualKeyCode@ultralight@@YAXHAEAVString@1@@Z")]
		private static extern void GetKey(int i, IntPtr id);

		public static void Main()
		{
			WindowCreateInfo windowCI = new()
			{
				WindowWidth = (int)Width,
				WindowHeight = (int)Height,
				WindowTitle = "UltralightNet.Veldrid.TestApp",
				X = 100,
				Y = 100
			};

			Sdl2Window window = VeldridStartup.CreateWindow(ref windowCI);

			GraphicsDeviceOptions options = new()
			{
				PreferStandardClipSpaceYDirection = true,
				PreferDepthRangeZeroToOne = true,
				SwapchainSrgbFormat = false
			};

			GraphicsDevice graphicsDevice = VeldridStartup.CreateGraphicsDevice(
				window,
				options,
				BACKEND);
			ResourceFactory factory = graphicsDevice.ResourceFactory;

			ResourceLayout basicQuadResourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("_texture",
						ResourceKind.TextureReadOnly,
						ShaderStages.Fragment
					)//,
				/*new ResourceLayoutElementDescription("_texture",
					ResourceKind.TextureReadOnly,
					ShaderStages.Fragment
				)*/
				)
			);

			GraphicsPipelineDescription mainPipelineDescription = new(
				BlendStateDescription.SingleAlphaBlend,
				new DepthStencilStateDescription(
					depthTestEnabled: false,
					depthWriteEnabled: false,
					comparisonKind: ComparisonKind.Never),
				new RasterizerStateDescription(
					cullMode: FaceCullMode.Back,
					fillMode: PolygonFillMode.Solid,
					frontFace: FrontFace.Clockwise,
					depthClipEnabled: true,
					scissorTestEnabled: false),
				PrimitiveTopology.TriangleStrip,
				new ShaderSetDescription(
					new VertexLayoutDescription[] {
						new VertexLayoutDescription(
							new VertexElementDescription(
								"vPos",
								VertexElementSemantic.TextureCoordinate,
								VertexElementFormat.Float2
							),
							new VertexElementDescription(
								"fUv",
								VertexElementSemantic.TextureCoordinate,
								VertexElementFormat.Float2
							)
						)
					}, factory.CreateFromSpirv(new(ShaderStages.Vertex, Encoding.UTF8.GetBytes(@"
#version 450
precision highp float;

layout(location = 0) in vec2 in_pos;
layout(location = 1) in vec2 in_uv;

layout(location = 0) out vec2 out_uv;

void main()
{
    //gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
    gl_Position = vec4(in_pos, 0, 1);
	//fUv = 0.5 * vPos.xy + vec2(0.5,0.5);
	out_uv = in_uv;
}
"), "main"),
new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(@"
#version 450
precision highp float;

layout(set=0, binding = 0) uniform sampler2D _texture;
//layout(binding = 0) uniform sampler _sampler;
//layout(binding = 1) uniform texture2D _texture;

layout(location = 0) in vec2 out_uv;

layout(location = 0) out vec4 out_Color;

void main()
{
	//out_Color = texture(sampler2D(_texture, _sampler), out_uv);
	out_Color = texture(_texture, out_uv);
}
"), "main"))
				),
				new ResourceLayout[] {
					basicQuadResourceLayout
				},
				graphicsDevice.SwapchainFramebuffer.OutputDescription
			);

			Pipeline pipeline = factory.CreateGraphicsPipeline(mainPipelineDescription);

			VeldridGPUDriver gpuDriver = new(graphicsDevice);

			ULPlatform.SetLogger(new() { log_message = (lvl, msg) => Console.WriteLine(msg) }); ;
			AppCoreMethods.ulEnablePlatformFontLoader();
			ULPlatform.SetGPUDriver(gpuDriver.GetGPUDriver());

			Renderer renderer = new(new ULConfig()
			{
				ResourcePath = "./resources/",
				UseGpu = true,
				ForceRepaint = false
			});

			View view = new(renderer, Width, Height, true, Session.DefaultSession(renderer), false);
			View cpuView = new(renderer, Width, Height, true, Session.DefaultSession(renderer), true);

			const string url = "https://github.com/SupinePandora43";

			view.URL = url;
			cpuView.URL = url;

			WebRequest request = WebRequest.CreateHttp("https://raw.githubusercontent.com/SupinePandora43/UltralightNet/ulPath_pipelines/SilkNetSandbox/assets/index.html");

			var response = request.GetResponse();
			var responseStream = response.GetResponseStream();
			StreamReader reader = new(responseStream);
			string htmlText = reader.ReadToEnd();

			//view.HTML = htmlText;
			//cpuView.HTML = htmlText;

			Texture cpuTexture = null;
			ResourceSet cpuTextureResourceSet = null;

			void CreateCPUTexture()
			{
				cpuTexture = factory.CreateTexture(new TextureDescription(cpuView.Width, cpuView.Height, 1, 1, 1, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));
				cpuTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(basicQuadResourceLayout, cpuTexture));
			}

			CreateCPUTexture();

			int x = 0;
			int y = 0;

			bool cpu = false;

			window.MouseMove += (mm) =>
			{
				x = (int)mm.MousePosition.X;
				y = (int)mm.MousePosition.Y;

				ULMouseEvent mouseEvent = new ULMouseEvent()
				{
					button = ULMouseEvent.Button.None,
					type = ULMouseEvent.Type.MouseMoved,
					x = x,
					y = y
				};

				view.FireMouseEvent(mouseEvent);
				cpuView.FireMouseEvent(mouseEvent);
			};
			window.MouseDown += (md) =>
			{
				Console.WriteLine($"Mouse Down {md.Down} {md.MouseButton}");
				if (md.MouseButton is MouseButton.Right) cpu = !cpu;
				if (md.MouseButton is MouseButton.Button1)
				{
					view.GoBack();
					cpuView.GoBack();
				}
				else if (md.MouseButton is MouseButton.Button2)
				{
					view.GoForward();
					cpuView.GoForward();
				}
				ULMouseEvent mouseEvent = new ULMouseEvent()
				{
					button = md.MouseButton == MouseButton.Left ? ULMouseEvent.Button.Left : ULMouseEvent.Button.Right,
					type = ULMouseEvent.Type.MouseDown,
					x = x,
					y = y
				};
				view.FireMouseEvent(mouseEvent);
				cpuView.FireMouseEvent(mouseEvent);
			};
			window.MouseUp += (mu) =>
			{
				Console.WriteLine($"Mouse up {mu.Down} {mu.MouseButton}");
				ULMouseEvent mouseEvent = new ULMouseEvent()
				{
					button = mu.MouseButton == MouseButton.Left ? ULMouseEvent.Button.Left : ULMouseEvent.Button.Right,
					type = ULMouseEvent.Type.MouseUp,
					x = x,
					y = y
				};
				view.FireMouseEvent(mouseEvent);
				cpuView.FireMouseEvent(mouseEvent);
			};
			window.MouseWheel += (mw) =>
			{
				ULScrollEvent scrollEvent = new ULScrollEvent()
				{
					type = ULScrollEvent.Type.ByPixel,
					deltaY = (int)mw.WheelDelta * 100
				};
				view.FireScrollEvent(scrollEvent);
				cpuView.FireScrollEvent(scrollEvent);
			};

			// idk why
			view.Focus();
			cpuView.Focus();

			window.KeyDown += (kd) =>
			{
				/*ULKeyEvent kv = new(ULKeyEventType.Char, 0, 0, 0, "a", "a", false, false, false);
				view.FireKeyEvent(kv);
				cpuView.FireKeyEvent(kv);
				*/
				IntPtr keyIdentifier = ULStringMarshaler.ManagedToNative("");

				GetKey(ULKeyCodes.GK_TAB, keyIdentifier);

				string managedKeyIdentifier = ULStringMarshaler.NativeToManaged(keyIdentifier);

				ULKeyEventModifiers modifiers = 0;

				if ((kd.Modifiers & ModifierKeys.Alt) is ModifierKeys.Alt) modifiers |= ULKeyEventModifiers.AltKey;
				if ((kd.Modifiers & ModifierKeys.Control) is ModifierKeys.Control) modifiers |= ULKeyEventModifiers.CtrlKey;
				if ((kd.Modifiers & ModifierKeys.Shift) is ModifierKeys.Shift) modifiers |= ULKeyEventModifiers.ShiftKey;
				if ((kd.Modifiers & ModifierKeys.Gui) is ModifierKeys.Gui) modifiers |= ULKeyEventModifiers.MetaKey;

				ULKeyEvent ked = new(ULKeyEventType.KeyDown, modifiers, ULKeyCodes.GK_TAB, 0, "", null, false, false, false);

				view.FireKeyEvent(ked);
				cpuView.FireKeyEvent(ked);
			};

			window.KeyUp += (ku) =>
			{
				IntPtr keyIdentifier = ULStringMarshaler.ManagedToNative("");
				GetKey(ULKeyCodes.GK_TAB, keyIdentifier);
				string managedKeyIdentifier = ULStringMarshaler.NativeToManaged(keyIdentifier);
				ULKeyEvent ked = new(ULKeyEventType.KeyUp, 0, ULKeyCodes.GK_TAB, 0, "", null, false, false, false);
				view.FireKeyEvent(ked);
				cpuView.FireKeyEvent(ked);
			};

			window.Resized += () =>
			{
				graphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);
				view.Resize((uint)window.Width, (uint)window.Height);
				cpuView.Resize((uint)window.Width, (uint)window.Height);
				CreateCPUTexture();
			};
			DeviceBuffer quadV = factory.CreateBuffer(new(4 * 4 * 4, BufferUsage.VertexBuffer));
			graphicsDevice.UpdateBuffer(quadV, 0, new Vector4[]
			{
				new(-1, 1f, 0, 0 ),
				new(1, 1, 1, 0 ),
				new(-1, -1, 0, 1 ),
				new(1, -1, 1, 1 ),
			});

			DeviceBuffer quadI = factory.CreateBuffer(new(sizeof(short) * 4, BufferUsage.IndexBuffer));
			graphicsDevice.UpdateBuffer(quadI, 0, new short[] { 0, 1, 2, 3 });

			CommandList commandList = factory.CreateCommandList();
			Stopwatch stopwatch = new();
			stopwatch.Start();


			view.SetDOMReadyCallback((user_data, caller, frame_id, is_main_frame, url) =>
			{
				Console.WriteLine("Dom is ready");

				// view.EvaluateScript("window.location = \"https://heeeeeeeey.com/\"", out string exception);

				view.SetDOMReadyCallback(null);
			});

			while (window.Exists)
			{
				renderer.Update();
				gpuDriver.time = stopwatch.ElapsedTicks / 1000f;
				renderer.Render();

				ULSurface surface = cpuView.Surface;
				ULBitmap bitmap = surface.Bitmap;

				ULIntRect dirty = surface.DirtyBounds;

				IntPtr pixels = bitmap.LockPixels();
				uint rowBytes = bitmap.RowBytes;
				uint height = bitmap.Height;
				uint width = bitmap.Width;
				uint bpp = bitmap.Bpp;
				if (rowBytes == width * bpp)
				{
					graphicsDevice.UpdateTexture(cpuTexture, pixels, (uint)bitmap.Size, 0, 0, 0, width, height, 1, 0, 0);
				}
				else
				{
					Console.WriteLine("stride");
					graphicsDevice.UpdateTexture(cpuTexture, Unstrider.Unstride(pixels, width, cpuView.Height, bpp, rowBytes - (width * bpp)), 0, 0, 0, width, height, 1, 0, 0);
				}
				surface.ClearDirtyBounds();
				bitmap.UnlockPixels();

				gpuDriver.Render();

				commandList.Begin();

				commandList.SetPipeline(pipeline);

				commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
				commandList.SetFullViewports();
				commandList.ClearColorTarget(0, new RgbaFloat(MathF.Sin(stopwatch.Elapsed.Milliseconds / 100f), 255, 0, 255));
				//commandList.ClearColorTarget(0, RgbaFloat.Blue);

				commandList.SetVertexBuffer(0, quadV);
				commandList.SetIndexBuffer(quadI, IndexFormat.UInt16);

				if (cpu)
				{
					commandList.SetGraphicsResourceSet(0, cpuTextureResourceSet);
				}
				else
					commandList.SetGraphicsResourceSet(0, gpuDriver.GetRenderTarget(view));

				commandList.DrawIndexed(
					4,
					1,
					0,
					0,
					0
				);

				commandList.End();

				graphicsDevice.SubmitCommands(commandList);
				graphicsDevice.SwapBuffers();
				window.PumpEvents();
			}
		}
	}
}