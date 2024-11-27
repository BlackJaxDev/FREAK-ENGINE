using OpenVR.NET.Devices;
using System.Numerics;
using Valve.VR;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models;

namespace XREngine.Components.Scene.Mesh
{
    public class VRControllerModelComponent : ModelComponent
    {
        public VRControllerModelComponent()
        {

        }

        private bool _leftHand = false;
        public bool LeftHand
        {
            get => _leftHand;
            set => SetField(ref _leftHand, value);
        }

        public bool IsLoaded => Model is not null;

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            if (IsLoaded)
                return;

            Engine.VRState.Api.DeviceDetected += Api_DeviceDetected;
            Task.Run(LoadModelAsync);
        }

        private void Api_DeviceDetected(VrDevice device)
        {
            if (IsLoaded)
                return;

            Task.Run(LoadModelAsync);
        }

        private async Task LoadModelAsync()
        {
            var model = LeftHand
                ? Engine.VRState.Api.LeftController?.Model
                : Engine.VRState.Api.RightController?.Model;
            if (model is null)
                return;

            var comps = model.Components;
            Model m = new();
            foreach (var comp in comps)
            {
                var subMesh = await LoadComponentAsync(comp);
                if (subMesh is not null)
                    m.Meshes.Add(subMesh);
            }
            Model = m;
            Engine.VRState.Api.DeviceDetected -= Api_DeviceDetected;
        }

        private async Task<SubMesh?> LoadComponentAsync(ComponentModel comp)
        {
            if (!comp.ModelName.EndsWith(".obj"))
                return null;

            XRMesh mesh = new();
            XRMaterial mat = new();

            List<Vertex> vertices = [];
            List<ushort> triangleIndices = [];
            List<XRTexture2D> textures = [];

            SubMesh? m = null;
            void OnError(EVRRenderModelError error, ComponentModel.Context context)
            {

            }
            async void AddTexture(ComponentModel.Texture texture)
            {
                var image = await texture.LoadImage(true);
                if (image is null)
                    return;

                textures.Add(new XRTexture2D(image));
            }
            void AddTriangle(short index0, short index1, short index2)
            {
                triangleIndices.Add((ushort)index0);
                triangleIndices.Add((ushort)index2);
                triangleIndices.Add((ushort)index1);
            }
            void AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
            {
                vertices.Add(new Vertex(position, normal, uv));
            }
            void End(ComponentModel.ComponentType type)
            {
                if (vertices.Count == 0 || triangleIndices.Count == 0)
                    return;

                if (triangleIndices.Max() >= vertices.Count)
                {
                    Debug.Out("Invalid triangle index detected in model component.");
                    return;
                }

                mesh = new XRMesh(vertices, triangleIndices);
                if (textures.Count > 0)
                    mat = XRMaterial.CreateLitTextureMaterial(textures[0]);
                else
                    mat = XRMaterial.CreateLitColorMaterial(ColorF4.Magenta);
                m = new (new SubMeshLOD(mat, mesh, 0.0f));
            }
            bool Begin(ComponentModel.ComponentType type)
            {
                return true;
            }
            await comp.LoadAsync(Begin, End, AddVertex, AddTriangle, AddTexture, OnError);
            return m;
        }

        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
        }
    }
}
