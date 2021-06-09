﻿using ImGuiNET;
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
            io.Fonts.AddFontFromFileTTF(@"NotoSans.ttf", 18);
            io.Fonts.AddFontFromFileTTF(@"TenorSans.ttf", 18);

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            while (window.Loop(ref backgroundColor))
            {
                ImGui.ShowStyleEditor();
            }
        }
    }
}
