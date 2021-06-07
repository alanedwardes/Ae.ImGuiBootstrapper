using ImGuiNET;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper.Tester
{
    internal static class Program
    {
        private static void Main()
        {
            var windowInfo1 = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program 1");

            using var window1 = new ImGuiWindow(windowInfo1);

            var windowInfo2 = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program 2");

            using var window2 = new ImGuiWindow(windowInfo2);

            while (true)
            {
                window1.StartFrame();

                ImGui.Begin("Wibble1", ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Text("Testing1");
                ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");
                ImGui.Text($"Mouse position: {ImGui.GetIO().MouseClicked[0]}");
                ImGui.End();

                ImGui.ShowDemoWindow();

                window1.EndFrame(new Vector3(0.45f, 0.55f, 0.6f));

                window2.StartFrame();

                ImGui.Begin("Wibble2", ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Text("Testing2");
                ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");
                ImGui.Text($"Mouse position: {ImGui.GetIO().MouseClicked[0]}");
                ImGui.End();

                ImGui.ShowDemoWindow();

                window2.EndFrame(new Vector3(0.45f, 0.55f, 0.6f));
            }
        }
    }
}
