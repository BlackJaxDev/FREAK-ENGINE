using Extensions;
using Silk.NET.Assimp;
using System.ComponentModel;
using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Components
{
    public abstract class BaseRenderableMesh3D : XRBase
    {
        public BaseRenderableMesh3D() { }
        public BaseRenderableMesh3D(IList<LOD> lods, ERenderPass renderPass, RenderInfo3D renderInfo, TransformBase component)
        {
            _transform = component;

            LODs = lods.Select(x =>
            {
                RenderableLOD lod = new()
                {
                    VisibleDistance = x.VisibleDistance,
                    Manager = x.Renderer,
                };
                if (lod.Manager.Material.GeometryShaders.Count == 0)
                {
                    lod.Manager.Material.Shaders.Add(XRShader.EngineShader("VisualizeNormal.gs", EShaderType.Geometry));
                    //lod.Manager.Material.Requirements = XRMaterial.UniformRequirements.NeedsCamera;
                }
                return lod;
            }).ToList();

            CurrentLODIndex = LODs.Count - 1;

            RenderInfo = renderInfo;
            //RenderCommand.RenderPass = renderPass;
        }

        protected TransformBase _transform;
        private Matrix4x4 _initialCullingVolumeMatrix;
        private RenderInfo3D _renderInfo;

        public RenderableLOD? CurrentLOD => LODs.IndexInRange(CurrentLODIndex) ? LODs[CurrentLODIndex] : (LODs.Count > 0 ? LODs[^1] : null);

        public int CurrentLODIndex { get; private set; }

        public XRWorldInstance? World => _transform?.SceneNode?.World;

        [Browsable(false)]
        [DisplayName("Levels Of Detail")]
        public List<RenderableLOD> LODs { get; private set; }

        public RenderInfo3D RenderInfo
        {
            get => _renderInfo;
            protected set
            {
                var old = _renderInfo?.CullingVolume;

                //if (_renderInfo != null)
                //    _renderInfo.CullingVolumeChanged -= CullingVolumeChanged;

                //_renderInfo = value ?? new RenderInfo3D();

                //if (_renderInfo != null)
                //    _renderInfo.CullingVolumeChanged += CullingVolumeChanged;

                //CullingVolumeChanged(old, _renderInfo?.CullingVolume);
            }
        }
        private void CullingVolumeChanged(IShape oldVolume, IShape newVolume)
        {
            //if (oldVolume != null)
            //    _transform.SocketTransformChanged -= UpdateCullingVolumeTransform;

            //if (newVolume != null)
            //{
            //    _initialCullingVolumeMatrix = newVolume.GetTransformMatrix() * _transform.InverseWorldMatrix;
            //    _transform.SocketTransformChanged += UpdateCullingVolumeTransform;
            //}
            //else
            //    _initialCullingVolumeMatrix = Matrix4x4.Identity;
        }
        //private void UpdateCullingVolumeTransform(ISocket comp)
        //{
        //    RenderInfo.CullingVolume.SetTransformMatrix(comp.WorldMatrix * _initialCullingVolumeMatrix);
        //    RenderInfo.OctreeNode?.ItemMoved(this);
        //}

        private void UpdateLOD(float viewDist)
        {
            while (true)
            {
                var currentLOD = CurrentLOD;
                if (currentLOD is null)
                    break;

                if (viewDist < currentLOD.VisibleDistance)
                {
                    if (CurrentLODIndex - 1 >= 0)
                        --CurrentLODIndex;
                    else
                        break;
                }
                else
                {
                    if (CurrentLODIndex + 1 < LODs.Count && viewDist >= LODs[CurrentLODIndex + 1].VisibleDistance)
                        ++CurrentLODIndex;
                    else
                        break;
                }
            }
        }

        public void Destroy()
        {
            foreach (var lod in LODs)
                lod.Manager.Destroy();
        }

        public RenderCommandMesh3D RenderCommand { get; } = new RenderCommandMesh3D(0);
        public void AddRenderables(RenderCommandCollection passes, XRCamera camera)
        {
            float distance = camera?.DistanceFromNearPlane(_transform?.WorldTranslation ?? Vector3.Zero) ?? 0.0f;

            if (!passes.IsShadowPass)
                UpdateLOD(distance);

            RenderCommand.Mesh = CurrentLOD?.Manager;
            RenderCommand.WorldMatrix = _transform.WorldMatrix;
            RenderCommand.RenderDistance = distance;

            passes.Add(RenderCommand);
        }

        protected void LODs_PostAnythingRemoved(LOD item)
        {
            LODs.RemoveAt(LODs.Count - 1);
        }
        protected void LODs_PostAnythingAdded(LOD item)
        {
            LODs.Add(new RenderableLOD()
            {
                VisibleDistance = item.VisibleDistance,
                Manager = item.Renderer,
            });
        }
    }
    public class StaticRenderableMesh : BaseRenderableMesh3D
    {
        [Browsable(false)]
        public IStaticSubMesh Mesh { get; set; }

        public StaticRenderableMesh(IStaticSubMesh mesh, TransformBase transform)
            : base(mesh.LODs, mesh.RenderPass, mesh.RenderInfo, transform)
        {
            Mesh = mesh;
            Mesh.LODs.PostAnythingAdded += LODs_PostAnythingAdded;
            Mesh.LODs.PostAnythingRemoved += LODs_PostAnythingRemoved;
        }

        public override string ToString() => "";
            //=> Mesh.Name;
    }
    public class SkeletalRenderableMesh : BaseRenderableMesh3D
    {
        public SkeletalRenderableMesh(ISkeletalSubMesh mesh, Skeleton skeleton, TransformBase transform)
            : base(mesh.LODs, mesh.RenderPass, mesh.RenderInfo, transform)
        {
            Mesh = mesh;
            Skeleton = skeleton;
            Mesh.LODs.PostAnythingAdded += LODs_PostAnythingAdded;
            Mesh.LODs.PostAnythingRemoved += LODs_PostAnythingRemoved;
        }

        private Bone _singleBind;
        private Skeleton _skeleton;

        public Bone SingleBind => _singleBind;

        [Browsable(false)]
        public ISkeletalSubMesh Mesh { get; set; }

        [Browsable(false)]
        public Skeleton Skeleton
        {
            get => _skeleton;
            set
            {
                _skeleton = value;
                //foreach (RenderableLOD m in LODs)
                //    m.Manager?.SkeletonChanged(_skeleton);
            }
        }
        public override string ToString() => "";
            //=> Mesh.Name;
    }
}
