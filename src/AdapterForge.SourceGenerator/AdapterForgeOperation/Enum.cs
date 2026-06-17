namespace AdapterForge.SourceGenerator.AdapterForgeOperation
{
    internal enum HttpMethod
    {
        NONE,
        GET,
        POST,
        PUT,
        PATH,
        DELETE
    }

    internal enum ContractTypeKind
    {
        Primitive,
        Complex,
        Enum,
        Collection
    }
}
