using XREngine.Core.Files;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.Models
{
    /// <summary>
    /// Represents various levels of detail for a mesh that can be rendered.
    /// </summary>
    public class SubMesh : XRAsset
    {
        public SortedSet<SubMeshLOD> LODs { get; } = new(new LODSorter());

        private AABB _bounds;
        private IVolume? cullingVolumeOverride;

        public AABB Bounds
        {
            get => _bounds;
            set => SetField(ref _bounds, value);
        }

        public IVolume? CullingVolumeOverride
        {
            get => cullingVolumeOverride;
            set => SetField(ref cullingVolumeOverride, value);
        }

        public SubMesh() { }

        public SubMesh(XRMesh? primitives, XRMaterial? material)
            : this(new SubMeshLOD(material, primitives, 0.0f)) { }

        public SubMesh(params SubMeshLOD[] lods)
            : this((IEnumerable<SubMeshLOD>)lods) { }

        public SubMesh(IEnumerable<SubMeshLOD> lods)
        {
            foreach (SubMeshLOD lod in lods)
                LODs.Add(lod);
            Bounds = CalculateBoundingBox();
        }

        /// <summary>
        /// Calculates the fully-encompassing aabb for this model based on each child mesh's aabb.
        /// </summary>
        public AABB CalculateBoundingBox()
        {
            AABB bounds = new();
            foreach (SubMeshLOD lod in LODs)
                if (lod.Mesh != null)
                    bounds = AABB.Union(bounds, lod.Mesh.Bounds);
            return bounds;
        }
    }

    public class LODSorter : IComparer<SubMeshLOD>
    {
        public int Compare(SubMeshLOD? x, SubMeshLOD? y)
        {
            if (x is null && y is null)
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            return x.MaxVisibleDistance.CompareTo(y.MaxVisibleDistance);
        }
    }
}
