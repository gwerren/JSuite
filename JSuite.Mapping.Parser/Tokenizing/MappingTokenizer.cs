namespace JSuite.Mapping.Parser.Tokenizing
{
    using System.Collections.Generic;

    public static class MappingTokenizer
    {
        private static readonly Tokenizer<TokenType> Tokeniser
            = new Tokenizer<TokenType>(TokenType.String)
                .Token(TokenType.Comment, @"(?<=^([^""\r\n]|""[^""\r\n]*"")*)//[^\r\n]*")
                .Token(TokenType.QuotedItem, @"""([^""]|""{2})+""")
                .Token(TokenType.Partial, "<<[a-zA-Z0-9_-]+>>")
                .Token(TokenType.Variable, @"\$\([a-zA-Z0-9_-]+\)")
                .Token(TokenType.LineContinuation, @"[\r\n]+[ \t\r\n]+[ \t]+")
                .Token(TokenType.NewLine, @"[\r\n]+")
                .Token(TokenType.Whitespace, @"[ \t]+")
                .Token(TokenType.PartialAssignment, "::")
                .Token(TokenType.And, "&&")
                .Token(TokenType.Or, @"\|\|")
                .Token(TokenType.Equals, "=")
                .Token(TokenType.Dot, @"\.")
                .Token(TokenType.OpenSquareBracket, @"\[")
                .Token(TokenType.CloseSquareBracket, @"\]")
                .Token(TokenType.OpenCurlyBracket, "{")
                .Token(TokenType.CloseCurlyBracket, "}")
                .Token(TokenType.Comma, ",")
                .Token(TokenType.QuestionMark, @"\?")
                .Token(TokenType.ExclaimationMark, @"\!")
                .Token(TokenType.OpenRoundBracket, @"\(")
                .Token(TokenType.CloseRoundBracket, @"\)")
                .Token(TokenType.Colon, ":")
                .Token(TokenType.Item, "[a-zA-Z0-9_-]+")
                .Token(TokenType.WildCard, @"\*");

        public static IEnumerable<Token<TokenType>> Tokenize(string script)
            => Tokeniser.Tokenize(script);
    }
}