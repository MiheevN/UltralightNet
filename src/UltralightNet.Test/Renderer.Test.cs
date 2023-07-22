using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace UltralightNet.Test;

[Collection("Renderer")]
[Trait("Category", "Renderer")]
public sealed class RendererTest
{
	private Renderer Renderer { get; }
	public RendererTest(RendererFixture fixture) => Renderer = fixture.Renderer;

	private static ULViewConfig ViewConfig => new()
	{
		IsAccelerated = false,
		IsTransparent = false
	};

	[Fact]
	public void SessionTest()
	{
		using Session session = Renderer.DefaultSession;
		Assert.Equal("default", session.Name);
		Assert.Equal("/default", session.DiskPath);

		using Session session1 = Renderer.CreateSession(false, "myses1");
		Assert.Equal("myses1", session1.Name);
		Assert.False(session1.IsPersistent);

		using Session session2 = Renderer.CreateSession(true, "myses2");
		Assert.Equal("myses2", session2.Name);
		Assert.True(session2.IsPersistent);

		Assert.True(session.Id != session1.Id && session1.Id != session2.Id);
	}

	[Fact]
	[Trait("Network", "Required")]
	public void GenericTest()
	{
		using View view = Renderer.CreateView(512, 512, ViewConfig);

		Assert.Equal(512u, view.Width);
		Assert.Equal(512u, view.Height);

		bool OnChangeTitle = false;
		bool OnChangeURL = false;

		view.OnChangeTitle += (title) =>
		{
			Assert.Contains("GitHub", title);
			OnChangeTitle = true;
		};

		view.OnChangeURL += (url) =>
		{
			Assert.Equal("https://github.com/", url);
			OnChangeURL = true;
		};

		view.URL = "https://github.com/";

		Stopwatch sw = Stopwatch.StartNew();

		while (view.URL == "")
		{
			if (sw.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Couldn't load page in 10 seconds.");

			Renderer.Update();
			Thread.Sleep(100);
		}

		Renderer.Render();

		Assert.Equal("https://github.com/", view.URL);
		Assert.Contains("GitHub", view.Title);
		Assert.True(OnChangeTitle);
		Assert.True(OnChangeURL);
	}

	[Fact]
	public void JSTest()
	{
		using View view = Renderer.CreateView(2, 2, ViewConfig);
		Assert.Equal("3", view.EvaluateScript("1+2", out string exception));
		Assert.True(string.IsNullOrEmpty(exception));
	}

	[Fact]
	public void HTMLTest()
	{
		using View view = Renderer.CreateView(512, 512, ViewConfig);
		view.HTML = "<html />";
	}

	[Fact]
	public void EventTest()
	{
		using View view = Renderer.CreateView(256, 256, ViewConfig);
		view.FireKeyEvent(new(ULKeyEventType.Char, ULKeyEventModifiers.ShiftKey, 0, 0, "A", "A", false, false, false));
		view.FireMouseEvent(new ULMouseEvent() { Type = ULMouseEventType.MouseDown, X = 100, Y = 100, Button = ULMouseEventButton.Left });
		view.FireScrollEvent(new() { Type = ULScrollEventType.ByPage, DeltaX = 23, DeltaY = 123 });
	}

	[Fact]
	public void MemoryTest()
	{
		Renderer.LogMemoryUsage();
		Renderer.PurgeMemory();
		Renderer.LogMemoryUsage();
	}
}
