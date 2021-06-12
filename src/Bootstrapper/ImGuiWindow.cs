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
        public GraphicsDevice GraphicsDevice { get; }
        /// <summary>
        /// Provides access to the underlying <see cref="Sdl2Window"/>.
        /// </summary>
        public Sdl2Window Window { get; }
        /// <summary>
        /// Provides access to the underlying <see cref="ImGuiRenderer"/>.
        /// </summary>
        public ImGuiRenderer Renderer { get; }

        private readonly CommandList _cl;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private float _lastTime;
        private bool _loopedOnce;
        private bool _startFrame = true;

        private ImGuiWindow((Sdl2Window, GraphicsDevice) windowAndGraphicsDevice)
        {
            Window = windowAndGraphicsDevice.Item1;
            GraphicsDevice = windowAndGraphicsDevice.Item2;

            Window.Resized += () =>
            {
                GraphicsDevice.MainSwapchain.Resize((uint)Window.Width, (uint)Window.Height);
                Renderer.WindowResized(Window.Width, Window.Height);
            };

            _cl = GraphicsDevice.ResourceFactory.CreateCommandList();
            Renderer = new ImGuiRenderer(GraphicsDevice, Window.Width, Window.Height);
        }

        /// <summary>
        /// Create a new window using the specified <see cref="Sdl2Window"/> and <see cref="Veldrid.GraphicsDevice"/>.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="graphicsDevice"></param>
        public ImGuiWindow(Sdl2Window window, GraphicsDevice graphicsDevice) : this((window, graphicsDevice))
        {
        }

        /// <summary>
        /// Create a new window on which to render ImgGui elements.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public ImGuiWindow(string windowTitle, int x = 50, int y = 50, int width = 1280, int height = 720) : this(new WindowCreateInfo(x, y, width, height, WindowState.Normal, windowTitle))
        {
        }

        /// <summary>
        /// Create a new window on which to render ImgGui elements using the specified <see cref="WindowCreateInfo"/>.
        /// </summary>
        /// <param name="windowCreateInfo"></param>
        public ImGuiWindow(WindowCreateInfo windowCreateInfo) : this(CreateWindowAndGraphicsDevice(windowCreateInfo, CreateDefaultDeviceOptions()))
        {
        }

        /// <summary>
        /// Create a new window on which to render ImgGui elements.
        /// </summary>
        /// <param name="windowCreateInfo"></param>
        /// <param name="graphicsDeviceOptions"></param>
        public ImGuiWindow(WindowCreateInfo windowCreateInfo, GraphicsDeviceOptions graphicsDeviceOptions) : this(CreateWindowAndGraphicsDevice(windowCreateInfo, graphicsDeviceOptions))
        {
        }

        private static GraphicsDeviceOptions CreateDefaultDeviceOptions() => new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true);

        private static (Sdl2Window, GraphicsDevice) CreateWindowAndGraphicsDevice(WindowCreateInfo windowCreateInfo, GraphicsDeviceOptions graphicsDeviceOptions)
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(windowCreateInfo, graphicsDeviceOptions, out var window, out var gd);
            return (window, gd);
        }

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
        public bool IsOpen => Window.Exists;

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
            if (!IsOpen)
            {
                return;
            }

            // Calculate delta time
            float currentTime = _sw.ElapsedMilliseconds;
            float deltaTime = currentTime - _lastTime;
            _lastTime = currentTime;

            Renderer.StartFrame(deltaTime / 1000, Window.PumpEvents());
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
            Renderer.EndFrame();

            _cl.Begin();
            _cl.SetFramebuffer(GraphicsDevice.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1f));
            Renderer.Render(_cl);
            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);

            _startFrame = true;
        }

        /// <summary>
        /// Dispose of the low-level resources used by the window.
        /// </summary>
        public void Dispose()
        {
            GraphicsDevice.WaitForIdle();
            Renderer.Dispose();
            _cl.Dispose();
            GraphicsDevice.Dispose();
        }
    }
}