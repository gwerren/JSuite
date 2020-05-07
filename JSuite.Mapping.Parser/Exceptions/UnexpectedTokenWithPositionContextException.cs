namespace JSuite.Mapping.Parser.Exceptions
{
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class UnexpectedTokenWithPositionContextException : UnexpectedTokenException
    {
        private UnexpectedTokenWithPositionContextException(
            UnexpectedTokenException source,
            int line,
            int column)
            : base(
                $"Unexpected token type '{source.TokenType}' found at line {line}, column {column} with value '{source.TokenValue}'.",
                source.TokenType,
                source.TokenValue,
                source.TokenStartIndex,
                source)
        {
            this.Line = line;
            this.Column = column;
        }

        public int Line { get; }

        public int Column { get; }

        public static UnexpectedTokenWithPositionContextException For(
            UnexpectedTokenException source,
            LineColumn? location)
        {
            return new UnexpectedTokenWithPositionContextException(
                source,
                location?.Line ?? 0,
                location?.Column ?? 0);
        }
    }
}