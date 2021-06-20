using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Veldrid.ImageSharp;

namespace Ae.ImGuiBootstrapper.Ae.ImGuiBootstrapper.HighDpiSample
{
    enum MonitorDpiType
    {
        MDT_EFFECTIVE_DPI = 0,
        MDT_ANGULAR_DPI = 1,
        MDT_RAW_DPI = 2,
    }

    enum MonitorOpts : uint
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002,
    }

    enum DPI_AWARENESS_CONTEXT : int
    {
        DPI_AWARENESS_CONTEXT_UNAWARE = 16,
        DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = 17,
        DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = 18,
        DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = 34
    }

    internal static class WindowsNative
    {
        [DllImport("Shcore.dll")]
        internal static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS value);

        [DllImport("shcore.dll")]
        internal static extern uint GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, MonitorOpts dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT dpiFlag);
    }

    internal enum PROCESS_DPI_AWARENESS
    {
        PROCESS_DPI_UNAWARE,
        PROCESS_SYSTEM_DPI_AWARE,
        PROCESS_PER_MONITOR_DPI_AWARE
    };

    /// <summary>
    /// A sample to show rendering using the <see cref="ImGuiRenderer"/> component directly using a custom render pipeline.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Only valid from Windows 10
                    WindowsNative.SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                }
                catch (EntryPointNotFoundException)
                {
                    try
                    {
                        // Only valid from Windows 8.1
                        WindowsNative.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
                    }
                    catch (EntryPointNotFoundException)
                    {
                        // We've exhausted all avenues
                    }
                }
            }

            using var window = new ImGuiWindow("ImGui.NET Sample Program");

            var io = ImGui.GetIO();

            const string fontFile = "NotoSans.ttf";
            const int fontSize = 22;

            io.Fonts.AddFontFromFileTTF(fontFile, fontSize);

            var loadImageTask = Task.Run(() =>
            {
                var image = new ImageSharpTexture("HighresScreenshot00001.jpg");

                using var texture = image.CreateDeviceTexture(window.GraphicsDevice, window.GraphicsDevice.ResourceFactory);
                return (window.Renderer.CreateTextureResources(texture), texture);
            });

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            var oldScale = 1f;

            while (window.Loop(ref backgroundColor))
            {
                var getMonitorId = WindowsNative.MonitorFromWindow(window.Window.Handle, MonitorOpts.MONITOR_DEFAULTTONEAREST);
                var getDpi = WindowsNative.GetDpiForMonitor(getMonitorId, MonitorDpiType.MDT_EFFECTIVE_DPI, out var x, out var y);

                var newScale = x / 96f;

                ImGui.Begin("Test", ImGuiWindowFlags.HorizontalScrollbar);
                ImGui.Text($"{newScale} {x} {y}");
                if (loadImageTask.IsCompleted)
                {
                    ImGui.Image(loadImageTask.Result.Item1, new Vector2(loadImageTask.Result.Item2.Width, loadImageTask.Result.Item2.Height) / 2);
                }
                ImGui.End();

                ImGui.ShowDemoWindow();

                if (oldScale != newScale)
                {
                    // Rebuild the font using the new scale
                    ImGui.GetIO().Fonts.Clear();
                    ImGui.GetIO().Fonts.AddFontFromFileTTF(fontFile, fontSize * newScale);
                    unsafe
                    {
                        ImGui.GetIO().NativePtr->FontDefault = null;
                    }
                    window.Renderer.RebuildFontTexture();

                    // Figure out the relative scale change
                    var scaleChange = newScale / oldScale;

                    // Scale all ImGui sizes
                    ImGui.GetStyle().ScaleAllSizes(scaleChange);

                    // Scale the window itself
                    window.Window.Width = (int)(window.Window.Width * scaleChange);
                    window.Window.Height = (int)(window.Window.Height * scaleChange);

                    oldScale = newScale;
                }
            }
        }
    }
}