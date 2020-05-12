namespace JSuite.Mapping.Parser.Exceptions
{
    using System.Collections.Generic;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class UnexpectedTokenException : BadTokensException
    {
        protected UnexpectedTokenException(string messagePrefix, IList<BadToken> tokens)
            : base(messagePrefix, tokens, null) { }

        public static UnexpectedTokenException For<TToken>(Token<TToken> token)
            => new UnexpectedTokenException("Unexpected token: ", new[] { BadToken.Create(token) });
    }
}