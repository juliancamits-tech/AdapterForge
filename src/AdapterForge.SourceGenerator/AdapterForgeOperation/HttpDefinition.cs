#nullable enable

namespace AdapterForge.SourceGenerator.AdapterForgeOperation
{
    internal class HttpDefinition
    {
        public HttpMethod Verb { get; set; } = HttpMethod.NONE;
        public string? Authorization { get; set; } = null;
        public string Summary { get; set; } = string.Empty;
    }
}
