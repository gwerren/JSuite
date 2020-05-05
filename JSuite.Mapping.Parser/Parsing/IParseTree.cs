﻿namespace JSuite.Mapping.Parser.Parsing
{
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Tokenizing;

    public interface IParseTree<TToken, TRule>
    {
        int CapturedTokenCount { get; }
    }

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
        public static int CapturedTokenCount<TToken, TRule>(
            this IEnumerable<IParseTree<TToken, TRule>> nodes)
            => nodes.Select(o => o.CapturedTokenCount).DefaultIfEmpty(0).Sum();

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
    }
}