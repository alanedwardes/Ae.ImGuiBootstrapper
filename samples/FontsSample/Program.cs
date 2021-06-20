using ImGuiNET;
using System.Numerics;

namespace Ae.ImGuiBootstrapper.FontsSample
{
    /// <summary>
    /// A sample to showcase custom fonts.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            using var window = new ImGuiWindow("ImGui.NET Sample Program");

            var io = ImGui.GetIO();

            // Fonts can be added pre-start (font texture built automatically)
            io.Fonts.AddFontFromFileTTF(@"NotoSans.ttf", 18);

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            while (window.Window.Exists)
            {
                window.StartFrame();

                ImGui.ShowStyleEditor();

                ImGui.Begin("Runtime Font Swapper");

                if (ImGui.Button("Load NotoSans.ttf@18"))
                {
                    // Fonts can be added while running
                    io.Fonts.AddFontFromFileTTF(@"NotoSans.ttf", 18);
                    window.Renderer.RebuildFontTexture();
                }

                if (ImGui.Button("Load TenorSans.ttf@18"))
                {
                    io.Fonts.AddFontFromFileTTF(@"TenorSans.ttf", 18);
                    window.Renderer.RebuildFontTexture();
                }

                if (ImGui.Button("Load TenorSans.ttf@52"))
                {
                    // The same font with a different size can be added too
                    io.Fonts.AddFontFromFileTTF(@"TenorSans.ttf", 52);
                    window.Renderer.RebuildFontTexture();
                }

                if (ImGui.Button("Clear Fonts"))
                {
                    // Fonts can also be removed while running
                    io.Fonts.Clear();
                    unsafe
                    {
                        // Unsafe required - https://github.com/mellinoe/ImGui.NET/issues/260
                        ImGui.GetIO().NativePtr->FontDefault = null;
                    }
                    window.Renderer.RebuildFontTexture();
                }

                ImGui.End();

                window.EndFrame(ref backgroundColor);
            }
        }
    }
}
