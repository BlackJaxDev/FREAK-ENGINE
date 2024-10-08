namespace XREngine.Data
{
    /// <summary>
    /// These are the extensions that will be recognized by the asset manager as 3rd-party loadable for this asset.
    /// </summary>
    /// <param name="extensions"></param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class XR3rdPartyExtensionsAttribute(params string[] extensions) : Attribute
    {
        /// <summary>
        /// These are the 3rd-party file extensions that this asset type can load.
        /// </summary>
        public string[] Extensions { get; } = extensions;
    }
}
