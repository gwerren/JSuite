namespace JSuite.Mapping.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Exceptions;
    using JSuite.Mapping.Parser.Parsing;
    using JSuite.Mapping.Parser.Parsing.Generic;
    using JSuite.Mapping.Parser.Tokenizing;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public static class Mapper
    {
        public static IList<IParseTree<TokenType, ParserRuleType>> ParseScript(string script)
        {
            try
            {
                return MappingTokenizer
                    .Tokenize(script)
                    .ApplyModifications()
                    .ToStatements()
                    .ApplyPartials()
                    .Parse()
                    .ToList();
            }
            catch (UnexpectedTokenException unexpectedToken)
            {
                LineColumn? location = null;
                try
                {
                    location = new TextIndexToLineColumnTranslator(script)
                        .Translate(unexpectedToken.TokenStartIndex);
                }
                catch (Exception) { /* Ignore exception since it is just trying to add context. */ }

                throw UnexpectedTokenWithPositionContextException.For(unexpectedToken, location);
            }
        }
    }
}
