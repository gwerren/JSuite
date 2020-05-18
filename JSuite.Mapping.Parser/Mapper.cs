namespace JSuite.Mapping.Parser
{
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Executing;
    using JSuite.Mapping.Parser.Parsing;
    using JSuite.Mapping.Parser.Parsing.Generic;
    using JSuite.Mapping.Parser.Tokenizing;
    using JSuite.Mapping.Parser.Tokenizing.Generic;
    using Newtonsoft.Json.Linq;

    public class Mapper
    {
        private readonly IList<MappingExecutor> mappings;

        public Mapper(string script)
        {
            this.mappings = ParseScript(script).Select(o => new MappingExecutor(o)).ToList();
        }

        public void Map(JObject target, JObject source)
        {
            foreach (var mapping in this.mappings)
                mapping.Execute(target, source);
        }

        private static IEnumerable<IParseTree<TokenType, ParserRuleType>> ParseScript(string script)
        {
            var translator = new TextIndexHelper(script);
            return MappingTokenizer
                .Tokenize(script)
                .ApplyModifications()
                .ToStatements()
                .ApplyPartials()
                .Parse(translator)
                .Validate(translator);
        }

        private class MappingExecutor
        {
            private readonly ExtractFromSource extract;
            private readonly UpdateTarget update;

            public MappingExecutor(IParseTree<TokenType, ParserRuleType> mappingDefinition)
            {
                this.extract = mappingDefinition.SourceExtractor();
                this.update = mappingDefinition.TargetUpdater();
            }

            public void Execute(JObject target, JObject source)
            {
                foreach (var extracted in this.extract(source))
                    this.update(target, extracted);
            }
        }
    }
}