namespace JSuite.Mapping.Parser.Exceptions
{
    using System.Collections.Generic;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class UndefinedVariablesException : BadTokensWithPositionContextException
    {
        private UndefinedVariablesException(
            string messagePrefix,
            IList<BadTokenWithPositionContext> tokens) : base(messagePrefix, tokens, null) { }

        public static UndefinedVariablesException For<TToken>(
            IList<Token<TToken>> tokens,
            TextIndexToLineColumnTranslator translator)
            => new UndefinedVariablesException(
                "Undefined variables found in target: ",
                TokenDetails(tokens, translator));
    }
}