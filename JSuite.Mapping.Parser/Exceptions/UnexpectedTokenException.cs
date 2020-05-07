namespace JSuite.Mapping.Parser.Exceptions
{
    using System;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class UnexpectedTokenException : Exception
    {
        protected UnexpectedTokenException(
            string message,
            string tokenType,
            string tokenValue,
            int tokenStartIndex,
            Exception innerException)
            : base(message, innerException)
        {
            this.TokenType = tokenType;
            this.TokenValue = tokenValue;
            this.TokenStartIndex = tokenStartIndex;
        }

        public string TokenType { get; }

        public string TokenValue { get; }

        public int TokenStartIndex { get; }

        public static UnexpectedTokenException For<TToken>(Token<TToken> token)
            => new UnexpectedTokenException(
                $"Unexpected token type '{token.Type}' found at index {token.StartIndex} with value '{token.Value}'.",
                token.Type.ToString(),
                token.Value,
                token.StartIndex,
                null);
    }
}