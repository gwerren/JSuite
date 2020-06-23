namespace JSuite.Mapping.Parser.Tokenizing
{
    using System.Collections.Generic;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public static class MappingTokenizer
    {
        private static readonly Tokenizer<TokenType> Tokeniser;

        static MappingTokenizer()
        {
            var nameChars = CharMatcher.AnyOf(
                CharMatcher.Range('a', 'z'),
                CharMatcher.Range('A', 'Z'),
                CharMatcher.Range('0', '9'),
                CharMatcher.AnyOf('_', '-'));

            var newLineChars = CharMatcher.AnyOf('\r', '\n');
            var whitespaceChars = CharMatcher.AnyOf(' ', '\t');

            Tokeniser = new Tokenizer<TokenType>(TokenType.String)
                .Token(
                    TokenType.Comment,
                    o => o
                        .Match(CharMatcher.Single('/')).CanStart()
                        .Then(CharMatcher.Single('/'))
                        .Then(CharMatcher.NoneOf(newLineChars)).CanRepeat().CanEnd())
                .Token(
                    TokenType.Comment,
                    o =>
                    {
                        var commentContent = o
                            .Match(CharMatcher.Single('/')).CanStart()
                            .Then(CharMatcher.Single('*'))
                            .Then(CharMatcher.NoneOf('*')).CanRepeat();

                        var closingStar = commentContent
                            .Then(CharMatcher.Single('*')).CanRepeat();

                        closingStar
                            .Then(CharMatcher.Single('/')).CanEnd();

                        closingStar.CanFollowWith(commentContent);
                    })
                .Token(
                    TokenType.QuotedItem,
                    o =>
                    {
                        var start = o.Match(CharMatcher.Single('"')).CanStart();
                        var end = start
                            .Then(CharMatcher.NoneOf('"')).CanRepeat()
                            .Then(CharMatcher.Single('"')).CanEnd();

                        end.CanFollowWith(start);
                        start.CanFollowWith(end);
                    })
                .Token(
                    TokenType.Partial,
                    o => o
                        .Match(CharMatcher.Single('<')).CanStart()
                        .Then(CharMatcher.Single('<'))
                        .Then(nameChars).CanRepeat()
                        .Then(CharMatcher.Single('>'))
                        .Then(CharMatcher.Single('>')).CanEnd())
                .Token(
                    TokenType.Variable,
                    o => o
                        .Match(CharMatcher.Single('$')).CanStart()
                        .Then(CharMatcher.Single('('))
                        .Then(nameChars).CanRepeat()
                        .Then(CharMatcher.Single(')')).CanEnd())
                .Token(
                    TokenType.LineContinuation,
                    o =>
                    {
                        var start = o.Match(newLineChars).CanRepeat().CanStart();
                        var end = o.Match(whitespaceChars).CanRepeat().CanEnd();
                        start.CanFollowWith(end);
                        end.CanFollowWith(start);
                    })
                .Token(
                    TokenType.NewLine,
                    o => o.Match(newLineChars).CanStart().CanRepeat().CanEnd())
                .Token(
                    TokenType.Whitespace,
                    o => o.Match(whitespaceChars).CanStart().CanRepeat().CanEnd())
                .Token(TokenType.PartialAssignment, "::")
                .Token(TokenType.And, "&&")
                .Token(TokenType.Or, "||")
                .Token(TokenType.Equals, '=')
                .Token(TokenType.Dot, '.')
                .Token(TokenType.Array, "[]")
                .Token(TokenType.OpenSquareBracket, '[')
                .Token(TokenType.CloseSquareBracket, ']')
                .Token(TokenType.OpenCurlyBracket, '{')
                .Token(TokenType.CloseCurlyBracket, '}')
                .Token(TokenType.Comma, ',')
                .Token(TokenType.QuestionMark, '?')
                .Token(TokenType.ExclaimationMark, '!')
                .Token(TokenType.OpenRoundBracket, '(')
                .Token(TokenType.CloseRoundBracket, ')')
                .Token(TokenType.Colon, ":")
                .Token(
                    TokenType.Item,
                    o => o.Match(nameChars).CanStart().CanRepeat().CanEnd())
                .Token(TokenType.WildCard, '*');
        }

        public static IEnumerable<Token<TokenType>> Tokenize(string script)
            => Tokeniser.Tokenize(script);
    }
}