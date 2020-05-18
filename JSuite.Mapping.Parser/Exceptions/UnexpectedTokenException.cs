namespace JSuite.Mapping.Parser.Exceptions
{
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class UnexpectedTokenException : BadTokensWithPositionContextException
    {
        private UnexpectedTokenException(string messagePrefix, BadTokenWithPositionContext token)
            : base(messagePrefix, new[] { token }, null) { }

        public static UnexpectedTokenException For<TToken>(Token<TToken> token, ITextIndexHelper translator)
            => new UnexpectedTokenException("Unexpected token: ", TokenDetails(token, translator));
    }
}