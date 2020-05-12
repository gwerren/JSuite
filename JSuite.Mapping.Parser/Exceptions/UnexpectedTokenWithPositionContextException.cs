namespace JSuite.Mapping.Parser.Exceptions
{
    using System;
    using System.Collections.Generic;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class UnexpectedTokenWithPositionContextException : BadTokensWithPositionContextException
    {
        protected UnexpectedTokenWithPositionContextException(
            string messagePrefix,
            IList<BadTokenWithPositionContext> tokens,
            Exception innerException) : base(messagePrefix, tokens, innerException) { }

        public static UnexpectedTokenWithPositionContextException For(
            UnexpectedTokenException source,
            TextIndexToLineColumnTranslator translator)
        {
            return new UnexpectedTokenWithPositionContextException(
                "Unexpected token: ",
                TokenDetails(source.Tokens, translator),
                source);
        }
    }
}