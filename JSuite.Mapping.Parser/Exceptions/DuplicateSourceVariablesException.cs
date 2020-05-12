namespace JSuite.Mapping.Parser.Exceptions
{
    using System.Collections.Generic;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class DuplicateSourceVariablesException : BadTokensWithPositionContextException
    {
        private DuplicateSourceVariablesException(
            string messagePrefix,
            IList<BadTokenWithPositionContext> tokens) : base(messagePrefix, tokens, null) { }

        public static DuplicateSourceVariablesException For<TToken>(
            IList<Token<TToken>> tokens,
            TextIndexToLineColumnTranslator translator)
            => new DuplicateSourceVariablesException(
                "Duplicate variable definitions found in source: ",
                TokenDetails(tokens, translator));
    }
}