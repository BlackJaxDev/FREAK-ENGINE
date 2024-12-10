namespace XREngine.Physics
{
    public class XRConstraintInfo2
    {
        public float Damping { get; set; }
        public float Erp { get; set; }
        public float Fps { get; set; }
        public List<float>? J1angularAxis { get; }
        public List<float>? J1linearAxis { get; }
        public List<float>? J2angularAxis { get; }
        public List<float>? J2linearAxis { get; }
        public List<float>? LowerLimit { get; }
        public int NumIterations { get; set; }
        public int Rowskip { get; set; }
        public List<float>? UpperLimit { get; }
        public List<float>? ConstraintError { get; }
        public List<float>? Cfm { get; }
    }
}
