using System.Collections.Generic;

namespace AdapterForge.SourceGenerator.AdapterForgeOperation
{
    internal class BuildContractTypeDefinition
    {
        public string Name { get; set; }

        public string FullName { get; set; }

        public ContractTypeKind Kind { get; set; }

        public bool Nullable { get; set; }
        public bool IsPrimitive { get; set; }

        public List<ContractPropertyDefinition> Properties { get; set; } = [];
    }
}
