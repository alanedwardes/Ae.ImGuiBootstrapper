using ImGuiNET;
using System;
using System.Numerics;

namespace Ae.ImGuiBootstrapper.DebugSample
{
    /// <summary>
    /// A sample based on a console app, to aid debugging pre-start problems.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("Creating window");

            using var window = new ImGuiWindow("ImGui.NET Sample Program");

            Console.WriteLine("Entering loop");

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            while (window.Loop(ref backgroundColor))
            {
                ImGui.ShowDemoWindow();
            }

            Console.WriteLine("Exiting loop");
        }
    }
}
