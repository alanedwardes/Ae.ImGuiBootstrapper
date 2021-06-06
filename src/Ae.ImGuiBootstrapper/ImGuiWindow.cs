using System;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Ae.ImGuiBootstrapper
{
    /// <summary>
    /// Provides a simple wrapper around a window to render ImgGui elements.
    /// </summary>
    public sealed class ImGuiWindow : IDisposable
    {
        private readonly Sdl2Window _window;
        private readonly GraphicsDevice _gd;
        private readonly CommandList _cl;
        private readonly ImGuiController _controller;

        /// <summary>
        /// Create a new window on which to render ImgGui elements.
        /// </summary>
        /// <param name="windowCreateInfo"></param>
        public ImGuiWindow(WindowCreateInfo windowCreateInfo)
        {
            var deviceOptions = new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true);

            VeldridStartup.CreateWindowAndGraphicsDevice(windowCreateInfo, deviceOptions, out _window, out _gd);
            
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };

            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
        }

        private bool _renderedFirstFrame;

        /// <summary>
        /// Should be called in a while loop, with ImgGui draw calls in the body of the loop.
        /// </summary>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public bool Loop(Vector3 backgroundColor)
        {
            if (_renderedFirstFrame)
            {
                EndFrame(backgroundColor);
            }

            StartFrame();
            _renderedFirstFrame = true;
            return _window.Exists;
        }

        private void StartFrame()
        {
            InputSnapshot snapshot = _window.PumpEvents();

            if (!_window.Exists)
            {
                return;
            }

            _controller.Update(1f / 60f, snapshot);
        }

        private void EndFrame(Vector3 backgroundColor)
        {
            if (!_window.Exists)
            {
                return;
            }

            _cl.Begin();
            _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1f));
            _controller.Render(_gd, _cl);
            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_gd.MainSwapchain);
        }

        /// <summary>
        /// Dispose of the low-level resources used by the window.
        /// </summary>
        public void Dispose()
        {
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }
    }
}