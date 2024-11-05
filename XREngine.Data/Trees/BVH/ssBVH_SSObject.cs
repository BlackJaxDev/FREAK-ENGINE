//// Copyright(C) David W. Jeske, 2013
//// Released to the public domain.

//using Silk.NET.OpenGLES;
//using System.Drawing;
//using System.Numerics;

//namespace SimpleScene.Util.ssBVH
//{

//    /// <summary>
//    /// An adaptor for ssBVH to understand SSObject nodes.
//    /// </summary>
//    public class SSObjectBVHNodeAdaptor : ISSBVHNodeAdaptor<SSObject>
//    {
//        Dictionary<SSObject, BVHNode<SSObject>> ssToLeafMap = new Dictionary<SSObject, BVHNode<SSObject>>();

//        public Vector3 Objectpos(SSObject obj)
//        {
//            return obj.Pos;
//        }
//        public float Radius(SSObject obj)
//        {
//            if (obj.localBoundingSphereRadius >= 0f)
//            {
//                // extract the object scale...
//                // use it to transform the object-space bounding-sphere radius into a world-space radius
//                return obj.worldBoundingSphereRadius;
//            }
//            else
//            {
//                return 1.0f;
//            }
//        }
//        public void CheckMap(SSObject obj)
//        {
//            if (!ssToLeafMap.ContainsKey(obj))
//            {
//                throw new Exception("missing map for shuffled child");
//            }
//        }
//        public void UnmapObject(SSObject obj)
//        {
//            ssToLeafMap.Remove(obj);
//        }
//        public void MapObjectToBVHLeaf(SSObject obj, BVHNode<SSObject> leaf)
//        {
//            // this allows us to be notified when an object moves, so we can adjust the BVH
//            obj.OnPositionOrSizeChanged += obj_OnChanged;

//            // TODO: add a hook to handle SSObject deletion... (either a weakref GC notify, or OnDestroy)

//            ssToLeafMap[obj] = leaf;
//        }
//        public BVHNode<SSObject> GetLeaf(SSObject obj)
//        {
//            return ssToLeafMap[obj];
//        }

//        // the SSObject has changed, so notify the BVH leaf to refit for the object
//        protected void obj_OnChanged(SSObject sender)
//        {
//            ssToLeafMap[sender].Refit_ObjectChanged(this, sender);
//        }

//        BVH<SSObject> _BVH;
//        public BVH<SSObject> BVH { get { return _BVH; } }

//        public void SetBVH(BVH<SSObject> BVH)
//        {
//            this._BVH = BVH;
//        }

//        public SSObjectBVHNodeAdaptor() { }
//    }

//    /// <summary>
//    /// This is a 3d render representation for an ssBVH tree.
//    /// </summary>
//    public class SSBVHRender : SSObject
//    {
//        BVH<SSObject> bvh;
//        public HashSet<BVHNode<SSObject>> highlightNodes = new HashSet<BVHNode<SSObject>>();

//        public SSBVHRender(BVH<SSObject> bvh)
//        {
//            this.bvh = bvh;
//            this.MainColor = Color4.Red;
//            this.renderState.lighted = false;
//        }

//        private static readonly SSVertex_Pos[] vertices = {
//            new SSVertex_Pos (0f, 0f, 0f), new SSVertex_Pos(1f, 0f, 0f), new SSVertex_Pos(1f, 1f, 0f), new SSVertex_Pos(0f, 1f, 0f),
//            new SSVertex_Pos (0f, 0f, 1f), new SSVertex_Pos(1f, 0f, 1f), new SSVertex_Pos(1f, 1f, 1f), new SSVertex_Pos(0f, 1f, 1f),
//        };
//        private static readonly ushort[] indices = {
//            0, 1, 1, 2, 2, 3, 3, 0, // face1
//            4, 5, 5, 6, 6, 7, 7, 4, // face2
//            0, 4, 1, 5, 2, 6, 3, 7  // interconnects
//        };

//        private static readonly SSVertexBuffer<SSVertex_Pos> vbo = new SSVertexBuffer<SSVertex_Pos>(vertices);
//        private static readonly SSIndexBuffer ibo = new SSIndexBuffer(indices, vbo);

//        public void renderCells(SSRenderConfig renderConfig, BVHNode<SSObject> n, ref SSAABB parentbox, int depth)
//        {
//            float nudge = 0f;

//            if (parentbox.Equals(n.box))
//            {
//                // attempt to nudge out of z-fighting
//                nudge = 0.2f;
//            }

//            if (highlightNodes.Contains(n))
//            {
//                if (n.gobjects == null)
//                {
//                    GL.Color4(Color.FromArgb(255, 25, 25, 100));
//                }
//                else
//                {
//                    GL.Color4(Color.Green);
//                }
//            }
//            else
//            {
//                if (n.gobjects == null)
//                {
//                    GL.Color4(Color.FromArgb(255, 20, 20, 20));
//                }
//                else
//                {
//                    GL.Color4(Color.DarkRed);
//                }
//            }

//            Vector3 nudgeVect = new Vector3(nudge);
//            Vector3 scale = n.box.Max - n.box.Min - 2f * nudgeVect;
//            Matrix4 mat = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(n.box.Min + nudgeVect);

//            GL.PushMatrix();
//            GL.MultMatrix(ref mat);
//            ibo.DrawElements(renderConfig, PrimitiveType.Lines, false);
//            GL.PopMatrix();

//            if (n.right != null) renderCells(renderConfig, n.right, ref n.box, depth: depth + 1);
//            if (n.left != null) renderCells(renderConfig, n.left, ref n.box, depth: depth + 1);
//        }

//        public override void Render(SSRenderConfig renderConfig)
//        {
//            if (renderConfig.drawingShadowMap) return;
//            base.Render(renderConfig);
//            SSShaderProgram.DeactivateAll();
//            GL.Disable(EnableCap.Texture2D);
//            GL.LineWidth(1.0f);

//            GL.MatrixMode(MatrixMode.Modelview);
//            ibo.Bind();
//            vbo.DrawBind(renderConfig);
//            this.renderCells(renderConfig, bvh.rootBVH, ref bvh.rootBVH.box, 0);
//            vbo.DrawUnbind();
//            ibo.Unbind();
//        }
//    }
//}
