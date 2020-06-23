using System;
using System.Collections.Generic;

namespace JSuite.Mapping.Parser.Tokenizing.Generic
{
    public class Tokenizer<TType>
    {
        private readonly TType defaultType;
        private readonly IList<ITokenMatcher> matchers = new List<ITokenMatcher>();

        public Tokenizer(TType defaultType) => this.defaultType = defaultType;

        public Tokenizer<TType> Token(TType type, char toMatch)
        {
            this.matchers.Add(new SingleCharTokenMatcher(type, toMatch));
            return this;
        }

        public Tokenizer<TType> Token(TType type, string sequence)
        {
            if (sequence.Length == 1)
                this.matchers.Add(new SingleCharTokenMatcher(type, sequence[0]));
            else if (sequence.Length != 0)
                this.matchers.Add(new SequenceTokenMatcher(type, sequence));

            return this;
        }

        public Tokenizer<TType> Token(TType type, Action<ITokenMatcherBuilder> configure)
        {
            var matcher = new ComplexTokenMatcher(type);
            configure(matcher);
            this.matchers.Add(matcher);
            return this;
        }

        public IReadOnlyList<Token<TType>> Tokenize(string input)
        {
            var builder = new TokensBuilder(this.defaultType);
            var longestTokenLength = 0;
            var longestTokenType = default(TType);
            var currentIndex = 0;
            while (currentIndex < input.Length)
            {
                foreach (var matcher in this.matchers)
                {
                    var matchLength = matcher.MatchLength(input, currentIndex);
                    if (matchLength > longestTokenLength)
                    {
                        longestTokenLength = matchLength;
                        longestTokenType = matcher.Type;
                    }
                }

                if (longestTokenLength != 0)
                {
                    builder.Add(
                        longestTokenType,
                        input.Substring(currentIndex, longestTokenLength),
                        currentIndex);

                    currentIndex += longestTokenLength;
                    longestTokenLength = 0;
                }
                else
                {
                    builder.UnMatched(input[currentIndex]);
                    ++currentIndex;
                }
            }

            return builder.GetTokens();
        }

        private interface ITokenMatcher
        {
            TType Type { get; }

            int MatchLength(string input, int inputIndex);
        }

        private class TokensBuilder
        {
            private readonly List<Token<TType>> tokens = new List<Token<TType>>();
            private readonly List<char> unmatched = new List<char>();
            private readonly TType unmatchedType;
            private int unmatchedStartIndex;

            public TokensBuilder(TType unmatchedType) => this.unmatchedType = unmatchedType;

            public void Add(TType type, string value, int startIndex)
            {
                this.UnMatchedToToken();

                this.tokens.Add(new Token<TType>(type, value, startIndex));
                this.unmatchedStartIndex = startIndex + value.Length;
            }

            public void UnMatched(char value) => this.unmatched.Add(value);

            public IReadOnlyList<Token<TType>> GetTokens()
            {
                this.UnMatchedToToken();
                return this.tokens;
            }

            private void UnMatchedToToken()
            {
                if (this.unmatched.Count != 0)
                {
                    this.tokens.Add(
                        new Token<TType>(
                            this.unmatchedType,
                            string.Concat(this.unmatched),
                            this.unmatchedStartIndex));

                    this.unmatched.Clear();
                }
            }
        }

        private class ComplexTokenMatcher : ITokenMatcher, ITokenMatcherBuilder
        {
            private readonly IList<TokenElementMatcher> elementMatchers = new List<TokenElementMatcher>();
            private readonly IList<int> allowedStartingElements = new List<int>();

            public ComplexTokenMatcher(TType type) => this.Type = type;

            public TType Type { get; }

            public ITokenElementMatcherBuilder Match(ICharMatcher matcher)
                => new TokenElementMatcherBuilder(matcher, this);

            public int MatchLength(string input, int inputIndex)
            {
                var allowedElements = this.allowedStartingElements;
                var currentCheckingLength = 1;
                var maxMatchLength = 0;
                while (inputIndex < input.Length && allowedElements.Count != 0)
                {
                    var matched = false;
                    foreach (var index in allowedElements)
                    {
                        var matcher = this.elementMatchers[index];
                        if (matcher.Matcher.IsMatch(input[inputIndex]))
                        {
                            if (matcher.CanEnd)
                                maxMatchLength = currentCheckingLength;

                            allowedElements = matcher.AllowedNext;
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                        return maxMatchLength;

                    ++inputIndex;
                    ++currentCheckingLength;
                }

                return maxMatchLength;
            }

            private class TokenElementMatcherBuilder : ITokenElementMatcherBuilder
            {
                private readonly ComplexTokenMatcher tokenMatcher;
                private readonly int matcherIndex;
                private readonly TokenElementMatcher matcher;

                public TokenElementMatcherBuilder(
                    ICharMatcher matcher,
                    ComplexTokenMatcher tokenMatcher)
                {
                    this.tokenMatcher = tokenMatcher;
                    this.matcherIndex = tokenMatcher.elementMatchers.Count;
                    this.matcher = new TokenElementMatcher(matcher);
                    tokenMatcher.elementMatchers.Add(this.matcher);
                }

                public void CanFollowWith(params ITokenElementMatcherBuilder[] followingMatchers)
                {
                    foreach (TokenElementMatcherBuilder following in followingMatchers)
                        this.matcher.AllowedNext.Add(following.matcherIndex);
                }

                public ITokenElementMatcherBuilder Then(ICharMatcher charMatcher)
                {
                    var elementMatcher = new TokenElementMatcherBuilder(charMatcher, this.tokenMatcher);
                    this.matcher.AllowedNext.Add(elementMatcher.matcherIndex);
                    return elementMatcher;
                }

                public ITokenElementMatcherBuilder CanStart()
                {
                    this.tokenMatcher.allowedStartingElements.Add(this.matcherIndex);
                    return this;
                }

                public ITokenElementMatcherBuilder CanEnd()
                {
                    this.matcher.CanEnd = true;
                    return this;
                }

                public ITokenElementMatcherBuilder CanRepeat()
                {
                    this.matcher.AllowedNext.Add(this.matcherIndex);
                    return this;
                }
            }

            private class TokenElementMatcher
            {
                public TokenElementMatcher(ICharMatcher matcher) { this.Matcher = matcher; }

                public ICharMatcher Matcher { get; }

                public IList<int> AllowedNext { get; } = new List<int>();

                public bool CanEnd { get; set; }
            }
        }

        private class SequenceTokenMatcher : ITokenMatcher
        {
            private readonly string sequence;

            public SequenceTokenMatcher(TType type, string sequence)
            {
                this.Type = type;
                this.sequence = sequence;
            }

            public TType Type { get; }

            public int MatchLength(string input, int inputIndex)
            {
                if (inputIndex + this.sequence.Length > input.Length)
                    return 0;

                for (int i = 0; i < sequence.Length; ++i)
                {
                    if (this.sequence[i] != input[inputIndex])
                        return 0;

                    ++inputIndex;
                }

                return sequence.Length;
            }
        }

        private class SingleCharTokenMatcher : ITokenMatcher
        {
            private readonly char toMatch;

            public SingleCharTokenMatcher(TType type, char toMatch)
            {
                this.Type = type;
                this.toMatch = toMatch;
            }

            public TType Type { get; }

            public int MatchLength(string input, int inputIndex)
                => input[inputIndex] == this.toMatch ? 1 : 0;
        }
    }
}