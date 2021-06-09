using ImGuiNET;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper.CustomGraphicsSample
{
    /// <summary>
    /// A sample to show creating a window and graphics backend manually.
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

            using var window = new ImGuiWindow(sdl2Window, graphicsDevice);

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            while (window.Loop(ref backgroundColor))
            {
                ImGui.Text($"Rendering with {graphicsBackend} and vsync = {vsync}");
                ImGui.ShowDemoWindow();
            }
        }
    }
}
