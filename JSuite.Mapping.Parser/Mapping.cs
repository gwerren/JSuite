namespace JSuite.Mapping.Parser
{
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Parsing;
    using JSuite.Mapping.Parser.Parsing.Generic;
    using JSuite.Mapping.Parser.Tokenizing;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public static class Mapping
    {
        public static IList<IParseTree<TokenType, ParserRuleType>> ParseScript(string script)
        {
            return MappingTokenizer
                .Tokenize(script)
                .ApplyModifications()
                .ToStatements()
                .ApplyPartials()
                .Parse(new TextIndexToLineColumnTranslator(script))
                .ToList();
        }
    }
}
