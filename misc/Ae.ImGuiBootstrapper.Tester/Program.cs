using ImGuiNET;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper.Tester
{
    class Program
    {
        static void Main()
        {
            var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program");

            using var window = new ImGuiWindow(windowInfo);

            while (window.Loop(new Vector3(0.45f, 0.55f, 0.6f)))
            {
                ImGui.Begin("wibble2");
                ImGui.Text("wibble");
                ImGui.End();
            }
        }
    }
}
