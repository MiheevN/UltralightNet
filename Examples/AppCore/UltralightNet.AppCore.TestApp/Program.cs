using System;
using System.IO;

namespace UltralightNet.AppCore.TestApp;

class Program
{
	static void Main()
	{
		AppCoreMethods.ulEnableDefaultLogger("./log.txt");
		AppCoreMethods.ulEnablePlatformFileSystem(Path.GetDirectoryName(typeof(Program).Assembly.Location));

		ULApp app = new(new ULSettings() { ForceCPURenderer = true }, new ULConfig() { });
		ULWindow window = new(app.MainMonitor, 512, 512, false, ULWindowFlags.Titled | ULWindowFlags.Resizable | ULWindowFlags.Maximizable);
		//ULWindow window1 = new(app.MainMonitor, 512, 512, false, ULWindowFlags.Titled | ULWindowFlags.Resizable);

		window.Title = "test title";

		ULOverlay overlay = new(window, window.Width, window.Height, 0, 0);

		window.SetResizeCallback((IntPtr user_data, ULWindow window, uint width, uint height) => overlay.Resize(width, height));
		window.SetCloseCallback((_, _) => app.Quit());

		//ULOverlay overlay1 = new(window1, 512, 512, 0, 0);

		//overlay1.View.HTML = "<html><body><p>123</p></body></html>";

		View view = overlay.View;
		//view.URL = "https://github.com/SupinePandora43/UltralightNet";

		view.OnFailLoading += (frame_id, is_main_frame, url, description, error_domain, error_code) => throw new Exception("Failed loading");

		bool l = false;

		view.OnFinishLoading += (frame_id, is_main_frame, url) => l = true;

		//view.HTML = "<html><body><p>123</p></body></html>";
		//view.URL = "https://vk.com/supinepandora43";
		view.URL = "https://www.youtube.com/watch?v=N1v4TjntTJI";
		//view.URL = "https://twitter.com/@supinepandora43";
		//while (!l) { app.Renderer.Update(); Thread.Sleep(20); }

		app.Run();
	}
}
