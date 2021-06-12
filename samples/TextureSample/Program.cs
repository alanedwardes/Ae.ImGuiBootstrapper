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

            using var texture1 = image.CreateDeviceTexture(window.GraphicsDevice, window.GraphicsDevice.ResourceFactory);
            var textureId1 = window.Renderer.CreateTextureResources(texture1);

            using var texture2 = image.CreateDeviceTexture(window.GraphicsDevice, window.GraphicsDevice.ResourceFactory);
            using var textureView2 = window.GraphicsDevice.ResourceFactory.CreateTextureView(texture2);
            var textureId2 = window.Renderer.CreateTextureViewResources(textureView2);

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            while (window.Loop(ref backgroundColor))
            {
                ImGui.Begin("Test Window", ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Image(textureId1, new Vector2(128));
                ImGui.Image(textureId2, new Vector2(128));
                ImGui.Text("An image loaded from disk");
                ImGui.End();
            }

            window.Renderer.DestroyTextureResources(texture1);
            window.Renderer.DestroyTextureViewResources(textureView2);
        }
    }
}
