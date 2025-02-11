using Extensions;
using Silk.NET.OpenGL;
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
                Data.SetBlockNameRequested -= SetUniformBlockName;
                Data.SetBlockIndexRequested -= SetBlockIndex;
                Data.BindRequested -= Bind;
                Data.UnbindRequested -= Unbind;
            }
            protected override void LinkData()
            {
                Data.PushDataRequested += PushData;
                Data.PushSubDataRequested += PushSubData;
                Data.SetBlockNameRequested += SetUniformBlockName;
                Data.SetBlockIndexRequested += SetBlockIndex;
                Data.BindRequested += Bind;
                Data.UnbindRequested += Unbind;
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
                try
                {
                    //TODO: get GL version
                    int glVer = 2;
                    uint index = GetBindingLocation(renderer);
                    if (index == uint.MaxValue)
                    {
                        Debug.LogWarning($"Failed to bind buffer {GetDescribingName()} to mesh renderer {renderer.GetDescribingName()}.");
                        renderer.VertexProgram.Data.Shaders.ForEach(x => Debug.Out(x?.Source?.Text ?? string.Empty));
                        return;
                    }
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
                                    //Api.BufferData(ToGLEnum(Data.Target), Data.Length, Data.Address.Pointer, ToGLEnum(Data.Usage));
                                    Api.BindBufferBase(ToGLEnum(Data.Target), index, BindingId);
                                    Unbind();
                                    break;
                            }

                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e, "Error binding buffer.");
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

                if (string.IsNullOrWhiteSpace(bindingName))
                    Debug.LogWarning($"{GetDescribingName()} has no binding name.");

                var vtx = renderer.VertexProgram;

                switch (Data.Target)
                {
                    case EBufferTarget.ArrayBuffer:

                        int location = vtx.GetAttributeLocation(bindingName);
                        if (location >= 0)
                            index = (uint)location;

                        break;
                    case EBufferTarget.ShaderStorageBuffer:
                        index = Data.BindingIndexOverride ?? Api.GetProgramResourceIndex(vtx.BindingId, GLEnum.ShaderStorageBlock, bindingName);
                        break;
                    case EBufferTarget.UniformBuffer:
                        index = Data.BindingIndexOverride ?? Api.GetProgramResourceIndex(vtx.BindingId, GLEnum.UniformBlock, bindingName);
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

            public void MapBufferData()
            {
                if (Data.ActivelyMapping.Contains(this))
                    return;
                
                uint id = BindingId;
                uint length = Data.Source!.Length;

                VoidPtr addr = Data.Source.Address;
                Api.NamedBufferStorage(id, length, ref addr, (uint)ToGLEnum(Data.StorageFlags));

                Data.ActivelyMapping.Add(this);

                Data.Source?.Dispose();
                Data.Source = new DataSource(Api.MapNamedBufferRange(id, IntPtr.Zero, length, (uint)ToGLEnum(Data.RangeFlags)), length);
            }

            public static GLEnum ToGLEnum(EBufferMapStorageFlags storageFlags)
            {
                GLEnum flags = 0;
                if (storageFlags.HasFlag(EBufferMapStorageFlags.Read))
                    flags |= GLEnum.MapReadBit;
                if (storageFlags.HasFlag(EBufferMapStorageFlags.Write))
                    flags |= GLEnum.MapWriteBit;
                if (storageFlags.HasFlag(EBufferMapStorageFlags.Persistent))
                    flags |= GLEnum.MapPersistentBit;
                if (storageFlags.HasFlag(EBufferMapStorageFlags.Coherent))
                    flags |= GLEnum.MapCoherentBit;
                if (storageFlags.HasFlag(EBufferMapStorageFlags.ClientStorage))
                    flags |= GLEnum.ClientStorageBit;
                return flags;
            }

            public static GLEnum ToGLEnum(EBufferMapRangeFlags rangeFlags)
            {
                GLEnum flags = 0;
                if (rangeFlags.HasFlag(EBufferMapRangeFlags.Read))
                    flags |= GLEnum.MapReadBit;
                if (rangeFlags.HasFlag(EBufferMapRangeFlags.Write))
                    flags |= GLEnum.MapWriteBit;
                if (rangeFlags.HasFlag(EBufferMapRangeFlags.Persistent))
                    flags |= GLEnum.MapPersistentBit;
                if (rangeFlags.HasFlag(EBufferMapRangeFlags.Coherent))
                    flags |= GLEnum.MapCoherentBit;
                if (rangeFlags.HasFlag(EBufferMapRangeFlags.InvalidateRange))
                    flags |= GLEnum.MapInvalidateRangeBit;
                if (rangeFlags.HasFlag(EBufferMapRangeFlags.InvalidateBuffer))
                    flags |= GLEnum.MapInvalidateBufferBit;
                if (rangeFlags.HasFlag(EBufferMapRangeFlags.FlushExplicit))
                    flags |= GLEnum.MapFlushExplicitBit;
                if (rangeFlags.HasFlag(EBufferMapRangeFlags.Unsynchronized))
                    flags |= GLEnum.MapUnsynchronizedBit;
                return flags;
            }

            public void UnmapBufferData()
            {
                if (!Data.ActivelyMapping.Contains(this))
                    return;
                
                Api.UnmapNamedBuffer(BindingId);
                Data.ActivelyMapping.Remove(this);
            }

            public void SetUniformBlockName(XRRenderProgram program, string blockName)
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