namespace JSuite.Mapping.Parser.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class BadTokensWithPositionContextException : ParsingException
    {
        protected BadTokensWithPositionContextException(
            string messagePrefix,
            IList<BadTokenWithPositionContext> tokens,
            Exception innerException)
            : base(messagePrefix + TokenDetailsString(tokens), innerException)
        {
            this.Tokens = tokens;
        }

        public IList<BadTokenWithPositionContext> Tokens { get; }

        protected static IList<BadTokenWithPositionContext> TokenDetails<TToken>(
            IList<Token<TToken>> tokens,
            ITextIndexHelper translator)
        {
            var details = new BadTokenWithPositionContext[tokens.Count];
            for (int i = 0; i < details.Length; ++i)
                details[i] = TokenDetails(tokens[i], translator);

            return details;
        }

        protected static BadTokenWithPositionContext TokenDetails<TToken>(
            Token<TToken> token,
            ITextIndexHelper translator)
        {
                var location = translator?.LinePosition(token.StartIndex);
                return new BadTokenWithPositionContext(
                    token.Type.ToString(),
                    token.Value,
                    token.StartIndex,
                    location);
        }

        protected static IList<BadTokenWithPositionContext> TokenDetails(
            IList<BadToken> tokens,
            ITextIndexHelper translator)
        {
            var details = new BadTokenWithPositionContext[tokens.Count];
            for (int i = 0; i < details.Length; ++i)
            {
                var baseDetails = tokens[i];
                var location = translator?.LinePosition(baseDetails.StartIndex);

                details[i] = new BadTokenWithPositionContext(baseDetails, location);
            }

            return details;
        }

        private static string TokenDetailsString(IList<BadTokenWithPositionContext> details)
            => string.Join(
                ", ",
                details.Select(
                    o => o.Location.HasValue
                        ? $"{o.Value} - [{o.Type}] line:{o.Location.Value.Line} column:{o.Location.Value.Column}"
                        : $"{o.Value} - [{o.Type}] index:{o.StartIndex}"));
    }

    public class BadTokenWithPositionContext : BadToken
    {
        public BadTokenWithPositionContext(BadToken baseDetails, LineColumn? location)
            : this(baseDetails.Type, baseDetails.Value, baseDetails.StartIndex, location) { }

        public BadTokenWithPositionContext(string type, string value, int startIndex, LineColumn? location)
            : base(type, value, startIndex) { this.Location = location; }

        public LineColumn? Location { get; }
    }
}