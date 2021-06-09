using ImGuiNET;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper.MultipleWindows
{
    /// <summary>
    /// A sample to showcase handling multiple windows (via multiple ImGui contexts).
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            var windowInfo1 = new WindowCreateInfo(128, 128, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program 1");

            using var window1 = new ImGuiWindow(windowInfo1);

            var windowInfo2 = new WindowCreateInfo(256, 256, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program 2");

            using var window2 = new ImGuiWindow(windowInfo2);

            while (window1.IsOpen || window2.IsOpen)
            {
                var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

                while (window1.Loop(ref backgroundColor))
                {
                    ImGui.ShowDemoWindow();
                }

                while (window2.Loop(ref backgroundColor))
                {
                    ImGui.Begin("Window 2", ImGuiWindowFlags.AlwaysAutoResize);
                    ImGui.Text("Testing2");
                    ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");
                    ImGui.End();
                }
            }
        }
    }
}
