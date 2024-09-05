namespace XREngine.Animation
{
    public interface ICartesianKeyframe<T> : IPlanarKeyframe where T : unmanaged
    {
        new T InValue { get; set; }
        new T OutValue { get; set; }
        new T InTangent { get; set; }
        new T OutTangent { get; set; }
    }
}
