namespace JSuite.Mapping.Parser.Parsing.Generic
{
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Exceptions;
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
        public static IParseTreeRule<TToken, TRule> Rule<TToken, TRule>(
            this IParseTree<TToken, TRule> tree,
            TRule ruleType)
        {
            if (!(tree is IParseTreeRule<TToken, TRule> rule))
                return null;
		
            return rule.Elements
                .OfType<IParseTreeRule<TToken, TRule>>()
                .SingleOrDefault(o => o.RuleType.Equals(ruleType));
        }

        public static Token<TToken> Token<TToken, TRule>(this IParseTree<TToken, TRule> tree)
        {
            while (true)
            {
                if (tree is IParseTreeToken<TToken, TRule> token)
                    return token.Token;

                var elements = ((IParseTreeRule<TToken, TRule>)tree).Elements;
                if (elements.Count != 1)
                    throw new ParsingException("Multiple tokens found when one was expected.");

                tree = elements[0];
            }
        }

        public static IEnumerable<Token<TToken>> Tokens<TToken, TRule>(
            this IEnumerable<IParseTree<TToken, TRule>> nodes)
            => nodes.TokenNodes().Select(o => o.Token);

        public static IEnumerable<Token<TToken>> Tokens<TToken, TRule>(
            this IParseTree<TToken, TRule> node)
            => node.TokenNodes().Select(o => o.Token);

        public static IEnumerable<IParseTreeToken<TToken, TRule>> TokenNodes<TToken, TRule>(
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
                    foreach (var token in ruleNode.Elements.TokenNodes())
                        yield return token;
                }
            }
        }

        public static IEnumerable<IParseTreeToken<TToken, TRule>> TokenNodes<TToken, TRule>(
            this IParseTree<TToken, TRule> node)
            => new[] { node }.TokenNodes();

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