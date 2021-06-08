using ImGuiNET;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper.TextureSample
{
    internal static class Program
    {
        private static void Main()
        {
            var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program");

            using var window = new ImGuiWindow(windowInfo);

            var image = new ImageSharpTexture("wibble.png");

            using var texture = image.CreateDeviceTexture(window.GraphicsDevice, window.ResourceFactory);

            var textureId = window.BindTexture(texture);

            while (window.Loop(new Vector3(0.45f, 0.55f, 0.6f)))
            {
                ImGui.Begin("Test Window", ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Image(textureId, new Vector2(128));
                ImGui.Text("An image loaded from disk");
                ImGui.End();
            }
        }
    }
}
