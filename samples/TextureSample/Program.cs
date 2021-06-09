using ImGuiNET;
using System.Numerics;
using Veldrid.ImageSharp;

namespace Ae.ImGuiBootstrapper.TextureSample
{
    /// <summary>
    /// A sample to show loading a texture and displaying it within ImGui.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            using var window = new ImGuiWindow("ImGui.NET Sample Program");

            var image = new ImageSharpTexture("wibble.png");

            using var texture = image.CreateDeviceTexture(window.GraphicsDevice, window.ResourceFactory);

            var textureId = window.BindTexture(texture);

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            while (window.Loop(ref backgroundColor))
            {
                ImGui.Begin("Test Window", ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Image(textureId, new Vector2(128));
                ImGui.Text("An image loaded from disk");
                ImGui.End();
            }
        }
    }
}
