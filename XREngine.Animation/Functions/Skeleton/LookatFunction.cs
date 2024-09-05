//using System.Numerics;

//namespace XREngine.Animation
//{
//    public class BoneLookatFunction : AnimationFunction
//    {
//        protected override void Execute(AnimationTree output, ISkeleton skeleton, object[] input)
//        {
//            string boneName = (string)input[0];
//            IBone bone = skeleton[boneName];
//            object arg2 = input[1];
//            Vector3 destPoint = 
//                (arg2 is Vector3 ? (Vector3)arg2 : 
//                (arg2 is Bone ? ((Bone)arg2).WorldMatrix.Translation : 
//                (arg2 is Matrix4x4 ? ((Matrix4x4)arg2).Translation :
//                Vector3.Zero)));
//            Vector3 sourcePoint = bone.WorldMatrix.Translation;
//            bone.FrameState.Rotation.Value = Quat.LookAt(sourcePoint, destPoint, Vector3.TransformVector(Vector3.Forward, bone.ParentSocket.WorldMatrix));
//        }

//        protected override AnimFuncValueInput[] GetValueInputs()
//        {
//            return new AnimFuncValueInput[]
//            {
//                //new AnimFuncValueInput("Bone", AnimArgType.String),
//                //new AnimFuncValueInput("Point", AnimArgType.Vector3, AnimArgType.Bone, AnimArgType.Matrix4),
//            };
//        }
//        protected override AnimFuncValueOutput[] GetValueOutputs()
//        {
//            return base.GetValueOutputs();
//        }
//    }
//}
