namespace SteelTube.Application.Synchronization.PreviewImport
{
    /// <summary>SAD 40: raw JSON text, already read from whatever file the user picked (Desktop's job, not Application's -- SAD 7.2).</summary>
    public sealed class PreviewImportQuery
    {
        public string PackageJson { get; set; }
    }
}