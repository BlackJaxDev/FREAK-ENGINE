using Silk.NET.OpenGL;
using System.Runtime.Intrinsics.X86;
using XREngine.Data;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        public class GLDataBuffer(OpenGLRenderer renderer, XRDataBuffer buffer) : GLObject<XRDataBuffer>(renderer, buffer)
        {
            protected override void UnlinkData()
            {
                Data.PushDataRequested -= PushData;
                Data.PushSubDataRequested -= PushSubData;
                Data.SetBlockNameRequested -= SetBlockName;
            }
            protected override void LinkData()
            {
                Data.PushDataRequested += PushData;
                Data.PushSubDataRequested += PushSubData;
                Data.SetBlockNameRequested += SetBlockName;
            }

            public override GLObjectType Type => GLObjectType.Buffer;

            protected internal override void PostGenerated()
            {
                var rend = Renderer.ActiveMeshRenderer;
                if (rend is null)
                {
                    var target = Data.Target;
                    if (target == EBufferTarget.ArrayBuffer)
                        Debug.LogWarning($"{GetDescribingName()} generated without a mesh renderer.");
                    return;
                }
                BindBuffer(rend);
            }

            private void BindBuffer(GLMeshRenderer renderer)
            {
                //TODO: get GL version
                int glVer = 2;
                uint index = GetBindingLocation(renderer);
                int componentType = (int)Data.ComponentType;
                uint componentCount = Data.ComponentCount;
                bool integral = Data.Integral;

                switch (glVer)
                {
                    case 0:
                        Bind();
                        if (Data.Target == EBufferTarget.ArrayBuffer)
                        {
                            Api.EnableVertexAttribArray(index);
                            void* addr = Data.Address;
                            if (integral)
                                Api.VertexAttribIPointer(index, (int)componentCount, GLEnum.Byte + componentType, 0, addr);
                            else
                                Api.VertexAttribPointer(index, (int)componentCount, GLEnum.Byte + componentType, Data.Normalize, 0, addr);
                        }
                        Unbind();
                        break;

                    case 1:

                        Api.BindVertexBuffer(index, BindingId, IntPtr.Zero, Data.ElementSize);

                        if (Data.Target == EBufferTarget.ArrayBuffer)
                        {
                            Api.EnableVertexAttribArray(index);
                            Api.VertexAttribBinding(index, index);
                            if (integral)
                                Api.VertexAttribIFormat(index, (int)componentCount, GLEnum.Byte + componentType, 0);
                            else
                                Api.VertexAttribFormat(index, (int)componentCount, GLEnum.Byte + componentType, Data.Normalize, 0);
                        }

                        break;

                    default:
                    case 2:

                        switch (Data.Target)
                        {
                            case EBufferTarget.ArrayBuffer:
                                {
                                    uint vaoId = renderer.BindingId;
                                    Api.EnableVertexArrayAttrib(vaoId, index);
                                    Api.VertexArrayBindingDivisor(vaoId, index, Data.InstanceDivisor);
                                    Api.VertexArrayAttribBinding(vaoId, index, index);
                                    if (integral)
                                        Api.VertexArrayAttribIFormat(vaoId, index, (int)componentCount, GLEnum.Byte + componentType, 0);
                                    else
                                        Api.VertexArrayAttribFormat(vaoId, index, (int)componentCount, GLEnum.Byte + componentType, Data.Normalize, 0);
                                    Api.VertexArrayVertexBuffer(vaoId, index, BindingId, 0, Data.ElementSize);
                                }
                                break;
                            case EBufferTarget.ShaderStorageBuffer:
                            case EBufferTarget.UniformBuffer:
                                Bind();
                                Api.BindBufferBase(ToGLEnum(Data.Target), index, BindingId);
                                Unbind();
                                break;
                        }

                        break;
                }

                if (Data.Mapped)
                    MapBufferData();
                else
                    PushData();
            }

            private uint GetBindingLocation(GLMeshRenderer renderer)
            {
                uint index = 0u;
                string bindingName = Data.BindingName;

                switch (Data.Target)
                {
                    case EBufferTarget.ArrayBuffer:

                        if (string.IsNullOrWhiteSpace(bindingName))
                            Debug.LogWarning($"{GetDescribingName()} has no binding name.");

                        if (renderer.VertexProgram is null)
                            Debug.LogWarning($"{GetDescribingName()} has no vertex program.");

                        int location = renderer.VertexProgram?.GetAttributeLocation(bindingName) ?? -1;
                        if (location >= 0)
                            index = (uint)location;

                        break;
                    case EBufferTarget.ShaderStorageBuffer:
                    case EBufferTarget.UniformBuffer:

                        if (string.IsNullOrWhiteSpace(bindingName))
                            Debug.LogWarning($"{GetDescribingName()} has no binding name.");

                        index = Api.GetUniformBlockIndex(BindingId, bindingName);

                        break;
                    default:
                        return 0;
                }

                return index;
            }

            /// <summary>
            /// Allocates and pushes the buffer to the GPU.
            /// </summary>
            public void PushData()
            {
                if (Data.ActivelyMapping.Contains(this))
                    return;

                void* addr = Data.Address;
                Api.NamedBufferData(BindingId, Data.Length, addr, ToGLEnum(Data.Usage));
            }

            public static GLEnum ToGLEnum(EBufferUsage usage) => usage switch
            {
                EBufferUsage.StaticDraw => GLEnum.StaticDraw,
                EBufferUsage.DynamicDraw => GLEnum.DynamicDraw,
                EBufferUsage.StreamDraw => GLEnum.StreamDraw,
                EBufferUsage.StaticRead => GLEnum.StaticRead,
                EBufferUsage.DynamicRead => GLEnum.DynamicRead,
                EBufferUsage.StreamRead => GLEnum.StreamRead,
                EBufferUsage.StaticCopy => GLEnum.StaticCopy,
                EBufferUsage.DynamicCopy => GLEnum.DynamicCopy,
                EBufferUsage.StreamCopy => GLEnum.StreamCopy,
                _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null),
            };

            /// <summary>
            /// Pushes the entire buffer to the GPU. Assumes the buffer has already been allocated using PushData.
            /// </summary>
            public void PushSubData()
                => PushSubData(0, Data.Length);

            /// <summary>
            /// Pushes the a portion of the buffer to the GPU. Assumes the buffer has already been allocated using PushData.
            /// </summary>
            public void PushSubData(int offset, uint length)
            {
                if (Data.ActivelyMapping.Contains(this))
                    return;

                if (!IsGenerated)
                    Generate();
                else
                {
                    void* addr = Data.Address;
                    Api.NamedBufferSubData(BindingId, offset, length, addr);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public enum EBufferMapAccess
            {
                Read = 0x0001,
                Write = 0x0002,
                ReadWrite = Read | Write,
                /// <summary>
                /// The client may request that the server read from or write to the buffer while it is mapped. 
                /// The client's pointer to the data store remains valid so long as the data store is mapped, even during execution of drawing or dispatch commands.
                /// If flags contains GL_MAP_PERSISTENT_BIT, it must also contain at least one of GL_MAP_READ_BIT or GL_MAP_WRITE_BIT.
                /// </summary>
                Persistent = 0x0040,
                /// <summary>
                /// Shared access to buffers that are simultaneously mapped for client access and are used by the server will be coherent, so long as that mapping is performed using glMapBufferRange. 
                /// That is, data written to the store by either the client or server will be immediately visible to the other with no further action taken by the application.
                /// In particular,
                /// If not set and the client performs a write followed by a call to the glMemoryBarrier command with the GL_CLIENT_MAPPED_BUFFER_BARRIER_BIT set, then in subsequent commands the server will see the writes.
                /// If set and the client performs a write, then in subsequent commands the server will see the writes.
                /// If not set and the server performs a write, the application must call glMemoryBarrier with the GL_CLIENT_MAPPED_BUFFER_BARRIER_BIT set and then call glFenceSync with GL_SYNC_GPU_COMMANDS_COMPLETE (or glFinish). Then the CPU will see the writes after the sync is complete.
                /// If set and the server does a write, the app must call glFenceSync with GL_SYNC_GPU_COMMANDS_COMPLETE(or glFinish). Then the CPU will see the writes after the sync is complete.
                /// If flags contains GL_MAP_COHERENT_BIT, it must also contain GL_MAP_PERSISTENT_BIT.
                /// </summary>
                Coherent = 0x0041,
                /// <summary>
                /// When all other criteria for the buffer storage allocation are met, 
                /// this bit may be used by an implementation to determine whether 
                /// to use storage that is local to the server 
                /// or to the client to serve as the backing store for the buffer.
                /// </summary>
                ClientStorage = 0x0042,
                InvalidateRange = 0x0004,
                InvalidateBuffer = 0x0008,
                FlushExplicit = 0x0010,
                Unsynchronized = 0x0020,
            }

            public void MapBufferData()
            {
                if (Data.ActivelyMapping.Contains(this))
                    return;
                
                uint id = BindingId;
                uint length = Data.Source!.Length;

                GLEnum bits = 
                    GLEnum.MapWriteBit |
                    GLEnum.MapReadBit |
                    GLEnum.MapPersistentBit |
                    GLEnum.MapCoherentBit |
                    GLEnum.ClientStorageBit;

                VoidPtr addr = Data.Source.Address;
                Api.NamedBufferStorage(id, length, ref addr, (uint)bits);

                Data.ActivelyMapping.Add(this);

                bits = GLEnum.MapPersistentBit |
                    GLEnum.MapCoherentBit |
                    GLEnum.MapReadBit |
                    GLEnum.MapWriteBit;

                Data.Source?.Dispose();
                Data.Source = new DataSource(Api.MapNamedBufferRange(id, IntPtr.Zero, length, (uint)bits), length);
            }

            public void UnmapBufferData()
            {
                if (!Data.ActivelyMapping.Contains(this))
                    return;
                
                Api.UnmapNamedBuffer(BindingId);
                Data.ActivelyMapping.Remove(this);
            }

            public void SetBlockName(XRRenderProgram program, string blockName)
            {
                var apiProgram = Renderer.GenericToAPI<GLRenderProgram>(program);
                if (apiProgram is null)
                    return;

                var bindingID = apiProgram.BindingId;
                if (bindingID == InvalidBindingId)
                    return;

                Bind();
                SetBlockIndex(Api.GetUniformBlockIndex(bindingID, blockName));
                Unbind();
            }

            public void SetBlockIndex(uint blockIndex)
            {
                if (blockIndex == uint.MaxValue)
                    return;

                Bind();
                Api.BindBufferBase(ToGLEnum(Data.Target), blockIndex, BindingId);
                Unbind();
            }

            protected internal override void PreDeleted()
                => UnmapBufferData();

            public void Bind()
                => Api.BindBuffer(ToGLEnum(Data.Target), BindingId);
            public void Unbind()
                => Api.BindBuffer(ToGLEnum(Data.Target), 0);
        }
    }
}