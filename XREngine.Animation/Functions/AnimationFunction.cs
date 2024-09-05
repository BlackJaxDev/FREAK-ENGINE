//namespace XREngine.Animation
//{
//    public abstract class AnimationFunction
//        : Function<AnimFuncValueInput, AnimFuncValueOutput, AnimFuncExecInput, AnimFuncExecOutput>
//    {
//        public bool HasExecuted => _results != null;
//        private object[] _results = null;
//        public T GetOutputValue<T>(int index)
//        {
//            if (_results is null)
//            {

//            }
//            return (T)_results[index];
//        }
//        public AnimationFunction() : base()
//        {

//        }
//        protected abstract void Execute(AnimationTree output, ISkeleton skeleton, object[] input);
//    }
//    public enum EAnimArgType : int
//    {
//        Invalid = -1,
//        String,
//        Enum,
//        Integer,
//        Decimal,
//        Vector2,
//        Vector3,
//        Vector4,
//        Matrix3,
//        Matrix4,
//        Bone,
//        Skeleton,
//        Rotator,
//        Quaternion,
//    }
//}
