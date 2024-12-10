namespace XREngine.Rendering
{
    public class XRMaterialInstance : XRMaterialBase
    {
        public XRMaterialInstance()
        {
            _inheritedMaterial = new XRMaterial();
            //_inheritedMaterial.Loaded += MaterialLoaded;
        }

        private XRMaterial? _inheritedMaterial;
        public XRMaterial? InheritedMaterial
        {
            get => _inheritedMaterial;
            set
            {
                //_inheritedMaterial.Loaded -= MaterialLoaded;
                _inheritedMaterial = value;
                //_inheritedMaterial.Loaded += MaterialLoaded;
            }
        }
        //private void MaterialLoaded(XRMaterial mat)
        //{

        //}
    }
}
