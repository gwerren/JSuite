using System.Collections.Generic;

namespace JSuite.Mapping.Parser.Tokenizing.Generic
{
    public interface ICharMatcher
    {
        bool IsMatch(char toCheck);
    }

    public static class CharMatcher
    {
        public static ICharMatcher Single(char toMatch)
            => new SingleCharMatcher(toMatch);

        public static ICharMatcher AnyOf(params char[] toMatch)
        {
            if (toMatch.Length == 1)
                return new SingleCharMatcher(toMatch[0]);

            return new IndividualCharMatcher(toMatch);
        }

        public static ICharMatcher AnyOf(params ICharMatcher[] matchers)
            => new AnyOfMatcher(matchers);

        public static ICharMatcher Range(char min, char max)
            => new CharRangeMatcher(min, max);

        public static ICharMatcher NoneOf(params char[] toMatch)
            => new NotMatcher(AnyOf(toMatch));

        public static ICharMatcher NoneOf(params ICharMatcher[] matchers)
            => matchers.Length == 1
                ? (ICharMatcher)new NotMatcher(matchers[0])
                : new NoneOfMatcher(matchers);

        private class SingleCharMatcher : ICharMatcher
        {
            private readonly char toMatch;

            public SingleCharMatcher(char toMatch) => this.toMatch = toMatch;

            public bool IsMatch(char toCheck) => toCheck == this.toMatch;
        }

        private class IndividualCharMatcher : ICharMatcher
        {
            private readonly IList<char> toMatch;

            public IndividualCharMatcher(IList<char> toMatch) => this.toMatch = toMatch;

            public bool IsMatch(char toCheck)
            {
                foreach (var item in this.toMatch)
                {
                    if (item == toCheck)
                        return true;
                }

                return false;
            }
        }

        private class CharRangeMatcher : ICharMatcher
        {
            private readonly char min;
            private readonly char max;

            public CharRangeMatcher(char min, char max)
            {
                this.min = min;
                this.max = max;
            }

            public bool IsMatch(char toCheck)
                => toCheck >= this.min && toCheck <= this.max;
        }

        private class AnyOfMatcher : ICharMatcher
        {
            private readonly IList<ICharMatcher> matchers;

            public AnyOfMatcher(IList<ICharMatcher> matchers) => this.matchers = matchers;

            public bool IsMatch(char toCheck)
            {
                foreach (var matcher in this.matchers)
                {
                    if (matcher.IsMatch(toCheck))
                        return true;
                }

                return false;
            }
        }

        private class NoneOfMatcher : ICharMatcher
        {
            private readonly IList<ICharMatcher> matchers;

            public NoneOfMatcher(IList<ICharMatcher> matchers) => this.matchers = matchers;

            public bool IsMatch(char toCheck)
            {
                foreach (var matcher in this.matchers)
                {
                    if (matcher.IsMatch(toCheck))
                        return false;
                }

                return true;
            }
        }

        private class NotMatcher : ICharMatcher
        {
            private readonly ICharMatcher matcher;

            public NotMatcher(ICharMatcher matcher) => this.matcher = matcher;

            public bool IsMatch(char toCheck) => !this.matcher.IsMatch(toCheck);
        }
    }
}