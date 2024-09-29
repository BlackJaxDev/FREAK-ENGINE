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

                        if (Data.Target == EBufferTarget.ArrayBuffer)
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
                }

                if (Data.Mapped)
                    MapBufferData();
                else
                    PushData();
            }

            private uint GetBindingLocation(GLMeshRenderer renderer)
            {
                uint index = 0u;
                if (Data.Target != EBufferTarget.ArrayBuffer)
                    return index;

                string bindingName = Data.BindingName;

                if (string.IsNullOrWhiteSpace(bindingName))
                    Debug.LogWarning($"{GetDescribingName()} has no binding name.");

                if (renderer.VertexProgram is null)
                    Debug.LogWarning($"{GetDescribingName()} has no vertex program.");

                int location = renderer.VertexProgram?.GetAttributeLocation(bindingName) ?? -1;
                if (location >= 0)
                    index = (uint)location;

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

            public void MapBufferData(
                bool read = true,
                bool write = true,
                bool persistent = true,
                bool coherent = true)
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
                Api.BindBufferBase(OpenGLRenderer.ToGLEnum(Data.Target), blockIndex, BindingId);
                Unbind();
            }

            protected internal override void PreDeleted()
                => UnmapBufferData();

            public void Bind()
                => Api.BindBuffer(OpenGLRenderer.ToGLEnum(Data.Target), BindingId);
            public void Unbind()
                => Api.BindBuffer(OpenGLRenderer.ToGLEnum(Data.Target), 0);
        }
    }
}