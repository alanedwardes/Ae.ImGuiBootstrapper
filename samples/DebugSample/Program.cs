using ImGuiNET;
using System;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper.DebugSample
{
    /// <summary>
    /// A sample based on a console app, to aid debugging pre-start problems.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program");

            Console.WriteLine("Creating window");

            using var window = new ImGuiWindow(windowInfo);

            Console.WriteLine("Entering loop");

            while (window.Loop(new Vector3(0.45f, 0.55f, 0.6f)))
            {
                ImGui.ShowDemoWindow();
            }

            Console.WriteLine("Exiting loop");
        }
    }
}
