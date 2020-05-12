namespace JSuite.Mapping.Parser.Exceptions
{
    public class PartialDefinitionException : ParsingException
    {
        private PartialDefinitionException(string message) : base(message) { }

        public static PartialDefinitionException DefinedMultipleTimes(string name)
            => new PartialDefinitionException($"The partial {name} is defined multiple times.");

        public static PartialDefinitionException DependsOnSelf(string name)
            => new PartialDefinitionException($"Partial {name} depends on itself.");

        public static PartialDefinitionException Missing(string name)
            => new PartialDefinitionException($"No definition found for partial {name}.");

        public static PartialDefinitionException CircularDependencies()
            => new PartialDefinitionException("Circular dependencies found in partial definitions.");
    }
}