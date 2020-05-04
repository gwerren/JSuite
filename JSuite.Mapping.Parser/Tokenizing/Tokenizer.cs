namespace JSuite.Mapping.Parser.Tokenizing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    internal class Tokenizer<TType>
    {
        private readonly IList<TokenType> tokenTypes = new List<TokenType>();
        private readonly TType defaultTokenType;

        public Tokenizer(TType defaultTokenType) => this.defaultTokenType = defaultTokenType;

        public Tokenizer<TType> Token(TType type, string matchingRegex)
        {
            this.tokenTypes.Add(new TokenType(type, matchingRegex));
            return this;
        }

        public IEnumerable<Token<TType>> Tokenize(string input)
        {
            IEnumerable<Token<TType>> tokens = new[] { new Token<TType>(this.defaultTokenType, input ?? string.Empty) };
            foreach (var type in this.tokenTypes)
                tokens = this.ExtractTokenType(tokens, type);

            return tokens.ToList();
        }

        private IEnumerable<Token<TType>> ExtractTokenType(
            IEnumerable<Token<TType>> tokens,
            TokenType toExtract)
        {
            var tokenType = toExtract.Type;
            var tokenMatcher = new Regex(toExtract.MatchingRegex, RegexOptions.Multiline);
            foreach (var token in tokens)
            {
                if (!token.Type.Equals(this.defaultTokenType))
                {
                    yield return token;
                    continue;
                }

                var matches = tokenMatcher.Matches(token.Value);
                if (matches.Count == 0)
                {
                    yield return token;
                    continue;
                }

                var currentIndex = 0;
                foreach (Match match in matches)
                {
                    if (currentIndex < match.Index)
                    {
                        yield return new Token<TType>(
                            this.defaultTokenType,
                            token.Value.Substring(currentIndex, match.Index - currentIndex));
                    }

                    yield return new Token<TType>(tokenType, match.Value);
                    currentIndex = match.Index + match.Length;
                }

                if (currentIndex < token.Value.Length)
                {
                    yield return new Token<TType>(
                        this.defaultTokenType,
                        token.Value.Substring(currentIndex, token.Value.Length - currentIndex));
                }
            }
        }

        private readonly struct TokenType
        {
            public TokenType(TType type, string matchingRegex)
            {
                this.Type = type;
                this.MatchingRegex = matchingRegex;
            }

            public TType Type { get; }

            public string MatchingRegex { get; }
        }
    }
}
