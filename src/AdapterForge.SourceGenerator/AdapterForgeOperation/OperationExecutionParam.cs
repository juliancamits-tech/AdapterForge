namespace AdapterForge.SourceGenerator.AdapterForgeOperation
{
    internal class OperationExecutionParam(string type, string text)
    {
        public string Type { get; set; } = type;
        public string Text { get; set; } = text;
    }

}
