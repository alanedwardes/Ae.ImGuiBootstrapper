using ImGuiNET;
using System.Diagnostics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper.AdvancedRendererSample
{
    /// <summary>
    /// A sample to show rendering using the <see cref="ImGuiRenderer"/> component directly using a custom render pipeline.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            var windowCreateInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Sample App");
            Sdl2Window sdl2Window = VeldridStartup.CreateWindow(windowCreateInfo);

            GraphicsBackend graphicsBackend = VeldridStartup.GetPlatformDefaultBackend();

            // Uncomment to force the Vulkan renderer
            // graphicsBackend = GraphicsBackend.Vulkan;

            bool vsync = true;
            var graphicsDeviceOptions = new GraphicsDeviceOptions(false, null, vsync, ResourceBindingModel.Improved, true, true);
            GraphicsDevice graphicsDevice = VeldridStartup.CreateGraphicsDevice(sdl2Window, graphicsDeviceOptions, graphicsBackend);

            var renderer = new ImGuiRenderer(graphicsDevice, sdl2Window.Width, sdl2Window.Height);

            sdl2Window.Resized += () =>
            {
                graphicsDevice.MainSwapchain.Resize((uint)sdl2Window.Width, (uint)sdl2Window.Height);
                renderer.WindowResized(sdl2Window.Width, sdl2Window.Height);
            };

            var commandList = graphicsDevice.ResourceFactory.CreateCommandList();

            var backgroundColor = new RgbaFloat(0.45f, 0.55f, 0.6f, 1f);

            float lastTime = 0;
            var sw = Stopwatch.StartNew();

            while (sdl2Window.Exists)
            {
                float currentTime = sw.ElapsedMilliseconds;
                float deltaTime = currentTime - lastTime;
                lastTime = currentTime;

                renderer.StartFrame(deltaTime / 1000, sdl2Window.PumpEvents());

                ImGui.Text($"Rendering with {graphicsBackend} and vsync = {vsync}");
                ImGui.ShowDemoWindow();

                renderer.EndFrame();

                commandList.Begin();
                commandList.SetFramebuffer(graphicsDevice.MainSwapchain.Framebuffer);
                commandList.ClearColorTarget(0, backgroundColor);
                renderer.Render(commandList);
                commandList.End();

                graphicsDevice.SubmitCommands(commandList);
                graphicsDevice.SwapBuffers(graphicsDevice.MainSwapchain);
            }
        }
    }
}