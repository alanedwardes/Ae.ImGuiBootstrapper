using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.IO;
using Veldrid;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace Ae.ImGuiBootstrapper
{
    /// <summary>
    /// A modified version of Veldrid.ImGui's ImGuiRenderer.
    /// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
    /// </summary>
    internal sealed class ImGuiController : IDisposable
    {
        private readonly GraphicsDevice _gd;

        // Veldrid objects
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private DeviceBuffer _projMatrixBuffer;
        private Texture _fontTexture;
        private TextureView _fontTextureView;
        private Shader _vertexShader;
        private Shader _fragmentShader;
        private ResourceLayout _layout;
        private ResourceLayout _textureLayout;
        private Pipeline _pipeline;
        private ResourceSet _mainResourceSet;
        private ResourceSet _fontTextureResourceSet;

        private readonly IntPtr _fontAtlasID = (IntPtr)1;
        private bool _controlDown;
        private bool _shiftDown;
        private bool _altDown;
        private bool _winKeyDown;

        private int _windowWidth;
        private int _windowHeight;
        private readonly IntPtr _context = ImGui.CreateContext();
        private readonly ImGuiIOPtr _io;
        private Vector2 _scaleFactor = Vector2.One;

        // Image trackers
        private readonly IDictionary<TextureView, IntPtr> _textureViewToPointerLookup = new Dictionary<TextureView, IntPtr>();
        private readonly IDictionary<Texture, TextureView> _textureToTextureViewLookup = new Dictionary<Texture, TextureView>();
        private readonly IDictionary<IntPtr, ResourceSet> _pointerToResourceSetLookup = new Dictionary<IntPtr, ResourceSet>();
        private readonly IList<IDisposable> _ownedResources = new List<IDisposable>();
        private int _lastAssignedID = 100;

        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public ImGuiController(GraphicsDevice gd, int width, int height)
        {
            _gd = gd;

            ImGui.SetCurrentContext(_context);
            _io = ImGui.GetIO();

            CreateDeviceResources();
            SetKeyMappings();

            WindowResized(width, height);
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
            _io.DisplaySize = new Vector2(_windowWidth, _windowHeight) / _scaleFactor;

            // Update the projection matrix
            _gd.UpdateBuffer(_projMatrixBuffer, 0, Matrix4x4.CreateOrthographicOffCenter(0f, _io.DisplaySize.X, _io.DisplaySize.Y, 0.0f, -1.0f, 1.0f));
        }

        public void CreateDeviceResources()
        {
            ResourceFactory factory = _gd.ResourceFactory;
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
            _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            _indexBuffer.Name = "ImGui.NET Index Buffer";

            _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            byte[] vertexShaderBytes = LoadEmbeddedShaderCode(_gd.ResourceFactory, "imgui-vertex", ShaderStages.Vertex);
            byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(_gd.ResourceFactory, "imgui-frag", ShaderStages.Fragment);
            _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, _gd.BackendType == GraphicsBackend.Metal ? "VS" : "main"));
            _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, _gd.BackendType == GraphicsBackend.Metal ? "FS" : "main"));

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
            };

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { _vertexShader, _fragmentShader }),
                new ResourceLayout[] { _layout, _textureLayout },
                _gd.MainSwapchain.Framebuffer.OutputDescription,
                ResourceBindingModel.Default);
            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout, _projMatrixBuffer, _gd.PointSampler));
        }

        /// <summary>
        /// Gets or creates a handle for a texture to be drawn with ImGui.
        /// Pass the returned handle to Image() or ImageButton().
        /// </summary>
        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
        {
            if (!_textureViewToPointerLookup.TryGetValue(textureView, out IntPtr binding))
            {
                ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));

                binding = GetNextImGuiBindingID();

                _textureViewToPointerLookup.Add(textureView, binding);
                _pointerToResourceSetLookup.Add(binding, resourceSet);
                _ownedResources.Add(resourceSet);
            }

            return binding;
        }

        private IntPtr GetNextImGuiBindingID()
        {
            return (IntPtr)_lastAssignedID++;
        }

        /// <summary>
        /// Gets or creates a handle for a texture to be drawn with ImGui.
        /// Pass the returned handle to Image() or ImageButton().
        /// </summary>
        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
        {
            if (!_textureToTextureViewLookup.TryGetValue(texture, out TextureView textureView))
            {
                textureView = factory.CreateTextureView(texture);
                _textureToTextureViewLookup.Add(texture, textureView);
                _ownedResources.Add(textureView);
            }

            return GetOrCreateImGuiBinding(factory, textureView);
        }

        public void ClearCachedImageResources()
        {
            foreach (IDisposable resource in _ownedResources)
            {
                resource.Dispose();
            }

            _ownedResources.Clear();
            _pointerToResourceSetLookup.Clear();
            _textureViewToPointerLookup.Clear();
            _textureToTextureViewLookup.Clear();
            _lastAssignedID = 100;
        }

        private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
        {
            switch (factory.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                {
                    string resourceName = name + ".hlsl.bytes";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                case GraphicsBackend.OpenGL:
                {
                    string resourceName = name + ".glsl";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                case GraphicsBackend.Vulkan:
                {
                    string resourceName = name + ".spv";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                case GraphicsBackend.Metal:
                {
                    string resourceName = name + ".metallib";
                    return GetEmbeddedResourceBytes(resourceName);
                }
                default:
                    throw new NotImplementedException();
            }
        }

        private byte[] GetEmbeddedResourceBytes(string resourceName)
        {
            Assembly assembly = typeof(ImGuiController).Assembly;
            using (Stream s = assembly.GetManifestResourceStream(resourceName))
            {
                byte[] ret = new byte[s.Length];
                s.Read(ret, 0, (int)s.Length);
                return ret;
            }
        }
        
        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public void RecreateFontDeviceTexture()
        {
            // Build
            _io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
            // Store our identifier
            _io.Fonts.SetTexID(_fontAtlasID);

            _fontTexture = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            _fontTexture.Name = "ImGui.NET Font Texture";
            _gd.UpdateTexture(_fontTexture, pixels, (uint)(bytesPerPixel * width * height), 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);
            _fontTextureView = _gd.ResourceFactory.CreateTextureView(_fontTexture);

            _fontTextureResourceSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTextureView));

            _pointerToResourceSetLookup[_fontAtlasID] = _fontTextureResourceSet;

            _io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Renders the ImGui draw list data.
        /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
        /// or index data has increased beyond the capacity of the existing buffers.
        /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
        /// </summary>
        public void Render(CommandList cl)
        {
            var currentContext = ImGui.GetCurrentContext();
            if (currentContext != _context)
            {
                throw new InvalidOperationException($"The context was changed between the EndFrame and Render calls. Expecting {_context}, got {currentContext}");
            }

            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData(), cl);
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void StartFrame(float deltaSeconds, InputSnapshot snapshot)
        {
            _io.DisplayFramebufferScale = _scaleFactor;
            _io.DeltaTime = deltaSeconds;

            UpdateImGuiInput(snapshot);

            ImGui.SetCurrentContext(_context);
            ImGui.NewFrame();
        }

        public void EndFrame()
        {
            ImGui.SetCurrentContext(_context);
            ImGui.EndFrame();
        }

        private void UpdateImGuiInput(InputSnapshot snapshot)
        {
            Vector2 mousePosition = snapshot.MousePosition / _scaleFactor;

            // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
            bool leftPressed = false;
            bool middlePressed = false;
            bool rightPressed = false;
            foreach (MouseEvent me in snapshot.MouseEvents)
            {
                if (me.Down)
                {
                    switch (me.MouseButton)
                    {
                        case MouseButton.Left:
                            leftPressed = true;
                            break;
                        case MouseButton.Middle:
                            middlePressed = true;
                            break;
                        case MouseButton.Right:
                            rightPressed = true;
                            break;
                    }
                }
            }

            _io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
            _io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
            _io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
            _io.MousePos = mousePosition;
            _io.MouseWheel = snapshot.WheelDelta;

            IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
            for (int i = 0; i < keyCharPresses.Count; i++)
            {
                char c = keyCharPresses[i];
                _io.AddInputCharacter(c);
            }

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent keyEvent = keyEvents[i];
                _io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
                if (keyEvent.Key == Key.ControlLeft)
                {
                    _controlDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.ShiftLeft)
                {
                    _shiftDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.AltLeft)
                {
                    _altDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.WinLeft)
                {
                    _winKeyDown = keyEvent.Down;
                }
            }

            _io.KeyCtrl = _controlDown;
            _io.KeyAlt = _altDown;
            _io.KeyShift = _shiftDown;
            _io.KeySuper = _winKeyDown;
        }

        private void SetKeyMappings()
        {
            _io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            _io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            _io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            _io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            _io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            _io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            _io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            _io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            _io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            _io.KeyMap[(int)ImGuiKey.Insert] = (int)Key.Insert;
            _io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            _io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
            _io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
            _io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            _io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            _io.KeyMap[(int)ImGuiKey.KeyPadEnter] = (int)Key.KeypadEnter;
            _io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            _io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            _io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            _io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            _io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            _io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        }

        private void RenderImDrawData(ImDrawDataPtr drawData, CommandList cl)
        {
            if (drawData.CmdListsCount == 0)
            {
                return;
            }

            uint totalVBSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
            if (totalVBSize > _vertexBuffer.SizeInBytes)
            {
                _gd.DisposeWhenIdle(_vertexBuffer);
                _vertexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            }

            uint totalIBSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
            if (totalIBSize > _indexBuffer.SizeInBytes)
            {
                _gd.DisposeWhenIdle(_indexBuffer);
                _indexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            }

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;
            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = drawData.CmdListsRange[i];

                cl.UpdateBuffer(_vertexBuffer, vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data, (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

                cl.UpdateBuffer(_indexBuffer, indexOffsetInElements * sizeof(ushort), cmd_list.IdxBuffer.Data, (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
            }

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _mainResourceSet);

            drawData.ScaleClipRects(_io.DisplayFramebufferScale);

            // Render command lists
            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = drawData.CmdListsRange[n];
                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (pcmd.TextureId != IntPtr.Zero)
                        {
                            cl.SetGraphicsResourceSet(1, _pointerToResourceSetLookup[pcmd.TextureId]);
                        }

                        cl.SetScissorRect(0, (uint)pcmd.ClipRect.X, (uint)pcmd.ClipRect.Y, (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X), (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));
                        cl.DrawIndexed(pcmd.ElemCount, 1, (uint)idx_offset, vtx_offset, 0);
                    }

                    idx_offset += (int)pcmd.ElemCount;
                }
                vtx_offset += cmd_list.VtxBuffer.Size;
            }
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _projMatrixBuffer.Dispose();
            _fontTexture.Dispose();
            _fontTextureView.Dispose();
            _vertexShader.Dispose();
            _fragmentShader.Dispose();
            _layout.Dispose();
            _textureLayout.Dispose();
            _pipeline.Dispose();
            _mainResourceSet.Dispose();

            foreach (IDisposable resource in _ownedResources)
            {
                resource.Dispose();
            }
        }
    }
}
