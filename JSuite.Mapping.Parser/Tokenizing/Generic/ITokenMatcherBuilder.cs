namespace JSuite.Mapping.Parser.Tokenizing.Generic
{
    public interface ITokenMatcherBuilder
    {
        ITokenElementMatcherBuilder Match(ICharMatcher matcher);
    }

    public interface ITokenElementMatcherBuilder
    {
        void CanFollowWith(params ITokenElementMatcherBuilder[] followingMatchers);
        ITokenElementMatcherBuilder Then(ICharMatcher charMatcher);
        ITokenElementMatcherBuilder CanStart();
        ITokenElementMatcherBuilder CanEnd();
        ITokenElementMatcherBuilder CanRepeat();
    }
}