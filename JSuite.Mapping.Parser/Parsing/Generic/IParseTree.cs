namespace JSuite.Mapping.Parser.Parsing.Generic
{
    using System.Collections.Generic;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public interface IParseTree<TToken, TRule> { }

    public interface IParseTreeRule<TToken, TRule> : IParseTree<TToken, TRule>
    {
        TRule RuleType { get; }

        IList<IParseTree<TToken, TRule>> Elements { get; }
    }

    public interface IParseTreeToken<TToken, TRule> : IParseTree<TToken, TRule>
    {
        Token<TToken> Token { get; }
    }

    public static class ParseTreeExtensions
    {
        public static IEnumerable<IParseTreeToken<TToken, TRule>> Tokens<TToken, TRule>(
            this IEnumerable<IParseTree<TToken, TRule>> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is IParseTreeToken<TToken, TRule> tokenNode)
                {
                    yield return tokenNode;
                }
                else if (node is IParseTreeRule<TToken, TRule> ruleNode)
                {
                    foreach (var token in ruleNode.Elements.Tokens())
                        yield return token;
                }
            }
        }

        public static IEnumerable<IParseTreeToken<TToken, TRule>> Tokens<TToken, TRule>(
            this IParseTree<TToken, TRule> node)
            => new[] { node }.Tokens();

        public static IEnumerable<IParseTree<TToken, TRule>> Elements<TToken, TRule>(
            this IEnumerable<IParseTree<TToken, TRule>> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is IParseTreeToken<TToken, TRule>)
                {
                    yield return node;
                }
                else if (node is IParseTreeRule<TToken, TRule> ruleNode)
                {
                    foreach (var element in ruleNode.Elements)
                        yield return element;
                }
            }
        }
    }
}