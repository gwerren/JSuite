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
            TextIndexToLineColumnTranslator translator;
            try
            {
                translator = new TextIndexToLineColumnTranslator(script);
            }
            catch (Exception)
            {
                /* Ignore exception since it is just trying to add context. */
                translator = null;
            }

            try
            {
                return MappingTokenizer
                    .Tokenize(script)
                    .ApplyModifications()
                    .ToStatements()
                    .ApplyPartials()
                    .Parse()
                    .Validate(translator)
                    .ToList();
            }
            catch (UnexpectedTokenException unexpectedToken)
            {
                throw UnexpectedTokenWithPositionContextException.For(unexpectedToken, translator);
            }
        }
    }
}
