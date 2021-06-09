using System;
using System.Diagnostics;
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
        /// <summary>
        /// Provides access to the underlying Veldrid <see cref="Veldrid.GraphicsDevice"/>.
        /// </summary>
        public GraphicsDevice GraphicsDevice => _gd;
        /// <summary>
        /// Provides access to the underlying Veldrid <see cref="Veldrid.ResourceFactory"/>.
        /// </summary>
        public ResourceFactory ResourceFactory => _gd.ResourceFactory;

        private readonly Sdl2Window _window;
        private readonly GraphicsDevice _gd;
        private readonly CommandList _cl;
        private readonly ImGuiController _controller;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private float lastTime = 0;
        private bool _loopedOnce;
        private bool _startFrame = true;
        private bool _resourcesCreated;

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
            _controller = new ImGuiController(_gd, _window.Width, _window.Height);
        }

        /// <summary>
        /// Binds the specified <see cref="Texture"/> to the graphics pipeline and return its <see cref="IntPtr"/> ID for use in ImGui.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public IntPtr BindTexture(Texture texture) => _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory, texture);

        /// <summary>
        /// Should be called in a while loop, with ImgGui draw calls in the body of the loop.
        /// </summary>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public bool Loop(ref Vector3 backgroundColor)
        {
            if (_loopedOnce)
            {
                EndFrameInternal(ref backgroundColor);
            }

            StartFrameInternal();
            _loopedOnce = true;
            return IsOpen;
        }

        /// <summary>
        /// Determines whether the window is open or has been closed.
        /// </summary>
        public bool IsOpen => _window.Exists;

        /// <summary>
        /// Start a new frame. This call should be followed by ImgGui draw calls.
        /// </summary>
        public void StartFrame()
        {
            if (!_startFrame)
            {
                throw new InvalidOperationException("EndFrame must be called before StartFrame can be called");
            }

            if (_loopedOnce)
            {
                throw new InvalidOperationException("StartFrame cannot be used with Loop");
            }

            StartFrameInternal();
        }

        private void StartFrameInternal()
        {
            if (!_resourcesCreated)
            {
                _controller.RecreateFontDeviceTexture();
                _resourcesCreated = true;
            }

            if (!IsOpen)
            {
                return;
            }

            // Calculate delta time
            float currentTime = _sw.ElapsedMilliseconds;
            float deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            _controller.StartFrame(deltaTime / 1000, _window.PumpEvents());
            _startFrame = false;
        }

        /// <summary>
        /// End the current frame after all ImGui draw calls.
        /// </summary>
        public void EndFrame(ref Vector3 backgroundColor)
        {
            if (!IsOpen)
            {
                return;
            }

            if (_startFrame)
            {
                throw new InvalidOperationException("StartFrame must be called before EndFrame can be called");
            }

            if (_loopedOnce)
            {
                throw new InvalidOperationException("EndFrame cannot be used with Loop");
            }

            EndFrameInternal(ref backgroundColor);
        }

        private void EndFrameInternal(ref Vector3 backgroundColor)
        {
            _controller.EndFrame();

            _cl.Begin();
            _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1f));
            _controller.Render(_cl);
            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_gd.MainSwapchain);

            _startFrame = true;
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