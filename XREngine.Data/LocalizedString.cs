namespace XREngine.Core
{
    public class LocalizedString
    {
        /// <summary>
        /// The namespace to look for the string key in.
        /// </summary>
        public string? Namespace { get; set; }
        /// <summary>
        /// The identifier to retrieve the desired string for any supported language.
        /// </summary>
        public string? StringKey { get; }
    }
    public class LocalizedStringTable
    {

    }
}
