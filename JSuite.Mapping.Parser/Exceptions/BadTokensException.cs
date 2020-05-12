namespace JSuite.Mapping.Parser.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class BadTokensException : ParsingException
    {
        protected BadTokensException(string messagePrefix, IList<BadToken> tokens, Exception innerException)
            : base(messagePrefix + TokenDetailsString(tokens), innerException) { this.Tokens = tokens; }

        public IList<BadToken> Tokens { get; }

        protected static IList<BadToken> TokenDetails<TToken>(IEnumerable<Token<TToken>> tokens)
            => tokens.Select(BadToken.Create).ToList();

        protected static string TokenDetailsString(IList<BadToken> details)
            => string.Join(", ", details.Select(o => $"{o.Value} - [{o.Type}] index:{o.StartIndex}"));
    }

    public class BadToken
    {
        public BadToken(string type, string value, int startIndex)
        {
            this.Type = type;
            this.Value = value;
            this.StartIndex = startIndex;
        }

        public string Type { get;  }

        public string Value { get; }

        public int StartIndex { get; }

        public static BadToken Create<TToken>(Token<TToken> token)
            => new BadToken(token.Type.ToString(), token.Value, token.StartIndex);
    }
}