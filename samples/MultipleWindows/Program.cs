using ImGuiNET;
using System.Numerics;

namespace Ae.ImGuiBootstrapper.MultipleWindows
{
    /// <summary>
    /// A sample to showcase handling multiple windows (via multiple ImGui contexts).
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            using var window1 = new ImGuiWindow("ImGui.NET Sample Program 1");

            using var window2 = new ImGuiWindow("ImGui.NET Sample Program 2");

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            while (window1.IsOpen || window2.IsOpen)
            {
                if (window1.Loop(ref backgroundColor))
                {
                    ImGui.ShowDemoWindow();
                }

                if (window2.Loop(ref backgroundColor))
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
