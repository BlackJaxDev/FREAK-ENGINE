﻿using Silk.NET.OpenGL;
using XREngine.Data;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        public class GLDataBuffer(OpenGLRenderer renderer, XRDataBuffer buffer) : GLObject<XRDataBuffer>(renderer, buffer)
        {
            public GLMeshRenderer? MeshRenderer { get; set; }

            protected override void UnlinkData()
            {
                base.UnlinkData();
                Data.PushDataRequested -= PushData;
                Data.PushSubDataRequested -= PushSubData;
                Data.SetBlockNameRequested -= SetBlockName;
            }
            protected override void LinkData()
            {
                base.LinkData();
                Data.PushDataRequested += PushData;
                Data.PushSubDataRequested += PushSubData;
                Data.SetBlockNameRequested += SetBlockName;
            }

            public override GLObjectType Type => GLObjectType.Buffer;

            protected internal override void PostGenerated()
            {
                if (MeshRenderer is null)
                    throw new Exception("MeshRenderer is null. This this the mesh renderer that the buffer will be linked to.");

                //TODO: get GL version
                int glVer = 2;
                int location = MeshRenderer?.VertexProgram?.GetUniformLocation(Data.BindingName) ?? -1;
                if (location == -1)
                    throw new Exception($"Uniform location for {Data.BindingName} not found.");

                uint index = (uint)location;
                int componentType = (int)Data.ComponentType;
                uint componentCount = Data.ComponentCount;
                bool integral = Data.Integral;

                switch (glVer)
                {
                    case 0:

                        Api.BindBuffer(OpenGLRenderer.ToGLEnum(base.Data.Target), BindingId);

                        if (Data.Target != EBufferTarget.ArrayBuffer)
                            break;

                        Api.EnableVertexAttribArray(index);
                        void* addr = Data.Address;
                        if (integral)
                            Api.VertexAttribIPointer(index, (int)componentCount, GLEnum.Byte + componentType, 0, addr);
                        else
                            Api.VertexAttribPointer(index, (int)componentCount, GLEnum.Byte + componentType, Data.Normalize, 0, addr);

                        break;

                    case 1:

                        Api.BindVertexBuffer(index, BindingId, IntPtr.Zero, Data.ElementSize);

                        if (Data.Target != EBufferTarget.ArrayBuffer)
                            break;

                        Api.EnableVertexAttribArray(index);
                        Api.VertexAttribBinding(index, index);
                        if (integral)
                            Api.VertexAttribIFormat(index, (int)componentCount, GLEnum.Byte + componentType, 0);
                        else
                            Api.VertexAttribFormat(index, (int)componentCount, GLEnum.Byte + componentType, Data.Normalize, 0);

                        break;

                    default:
                    case 2:

                        if (Data.Target != EBufferTarget.ArrayBuffer)
                            break;

                        uint vaoId = MeshRenderer.BindingId;
                        Api.EnableVertexArrayAttrib(vaoId, index);
                        Api.VertexArrayBindingDivisor(vaoId, index, Data.InstanceDivisor);
                        Api.VertexArrayAttribBinding(vaoId, index, index);
                        if (integral)
                            Api.VertexArrayAttribIFormat(vaoId, index, (int)componentCount, GLEnum.Byte + componentType, 0);
                        else
                            Api.VertexArrayAttribFormat(vaoId, index, (int)componentCount, GLEnum.Byte + componentType, Data.Normalize, 0);
                        Api.VertexArrayVertexBuffer(vaoId, index, BindingId, IntPtr.Zero, Data.ElementSize);

                        break;
                }

                if (Data.Mapped)
                    MapBufferData();
                else
                    PushData();
            }

            /// <summary>
            /// Allocates and pushes the buffer to the GPU.
            /// </summary>
            public void PushData()
            {
                if (Data.ActivelyMapping.Contains(this))
                    return;

                if (!IsGenerated)
                    Generate();
                else
                {
                    VoidPtr addr = Data.Address;
                    Api.NamedBufferData(BindingId, Data.Length, ref addr, ToGLEnum(Data.Usage));
                }
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
                    var addr = Data.Address;
                    Api.NamedBufferSubData(BindingId, offset, length, ref addr);
                }
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
                => SetBlockIndex(Api.GetUniformBlockIndex(Renderer.GenericToAPI<GLRenderProgram>(program).BindingId, blockName));

            public void SetBlockIndex(uint blockIndex)
                => Api.BindBufferBase(OpenGLRenderer.ToGLEnum(base.Data.Target), blockIndex, BindingId);

            protected internal override void PreDeleted()
                => UnmapBufferData();
        }
    }
}