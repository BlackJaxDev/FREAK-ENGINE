namespace XREngine.Animation
{
    public interface IKeyframe
    {
        Type ValueType { get; }
        float Second { get; set; }
    }
}
