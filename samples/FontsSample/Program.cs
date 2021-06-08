using ImGuiNET;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper.FontsSample
{
    internal static class Program
    {
        private static void Main()
        {
            var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program");

            using var window = new ImGuiWindow(windowInfo);

            var io = ImGui.GetIO();
            io.Fonts.AddFontFromFileTTF(@"NotoSans.ttf", 18);
            io.Fonts.AddFontFromFileTTF(@"TenorSans.ttf", 18);

            while (window.Loop(new Vector3(0.45f, 0.55f, 0.6f)))
            {
                ImGui.ShowStyleEditor();
            }
        }
    }
}
