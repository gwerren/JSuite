namespace JSuite.Mapping.Parser.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Parsing.Generic;
    using JSuite.Mapping.Parser.Tokenizing;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public static class MappingParser
    {
        private static readonly IList<TokenType> AllTokenTypes
            = Enum.GetValues(typeof(TokenType)).Cast<TokenType>().ToList();

        private static readonly Parser<TokenType, ParserRuleType> Parser
            = new Parser<TokenType, ParserRuleType>()
                .Rule(
                    ParserRuleType.Mapping,
                    o => o
                        .With(ParserRuleType.Target).AtMostOnce()
                        .Then(TokenType.Equals).Once()
                        .Then(ParserRuleType.Source).AtMostOnce())
                .ConfigureTarget()
                .ConfigureSource()
                .WithRoot(ParserRuleType.Mapping);

        public static IParseTree<TokenType, ParserRuleType> Parse(
            this IList<Token<TokenType>> tokens)
            => Parser.Parse(tokens);

        public static IEnumerable<IParseTree<TokenType, ParserRuleType>> Parse(
            this IEnumerable<IList<Token<TokenType>>> tokens)
            => tokens.Select(Parser.Parse);

        private static Parser<TokenType, TRule> RuleMatchNoneOf<TRule>(
            this Parser<TokenType, TRule> parser,
            TRule type,
            params TokenType[] tokens)
        {
            return parser.RuleMatchOneOf(type, AllTokenTypes.Except(tokens).ToArray());
        }

        private static Parser<TokenType, ParserRuleType> ConfigureTarget(
            this Parser<TokenType, ParserRuleType> parser)
        {
            return parser
                .Rule(
                    ParserRuleType.Target,
                    o => o
                        .With(ParserRuleType.TFirstNode).Once()
                        .Then(ParserRuleType.TSubsequentNode).ZeroOrMore())
                .Rule(
                    ParserRuleType.TFirstNode,
                    o => o.With(ParserRuleType.TNode).Once(),
                    o => o.With(ParserRuleType.TIndexedNode).Once())
                .Rule(
                    ParserRuleType.TSubsequentNode,
                    o => o
                        .With(TokenType.Dot).Once()
                        .Then(ParserRuleType.TNode).Once(),
                    o => o.With(ParserRuleType.TIndexedNode).Once())
                .Rule(
                    ParserRuleType.TNode,
                    o => o
                        .With(ParserRuleType.TPathElement).Once()
                        .Then(ParserRuleType.TNodeModifiers).Once())
                .Rule(
                    ParserRuleType.TPathElement,
                    o => o.With(TokenType.Item).Once(),
                    o => o.With(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.TIndexedNode,
                    o => o
                        .With(TokenType.OpenSquareBracket).Once()
                        .Then(ParserRuleType.TIndexedNodeContent).ZeroOrMore()
                        .Then(TokenType.CloseSquareBracket).Once()
                        .Then(ParserRuleType.TNodeModifiers).Once())
                .RuleMatchNoneOf(
                    ParserRuleType.TIndexedNodeContent,
                    TokenType.CloseSquareBracket)
                .Rule(
                    ParserRuleType.TNodeModifiers,
                    o => o
                        .With(ParserRuleType.TPropertyValue).AtMostOnce()
                        .Then(ParserRuleType.TConditionalModifier).AtMostOnce())
                .Rule(
                    ParserRuleType.TConditionalModifier,
                    o => o.With(TokenType.QuestionMark).Once(),
                    o => o.With(TokenType.ExclaimationMark).Once())
                .Rule(
                    ParserRuleType.TPropertyValue,
                    o => o
                        .With(TokenType.OpenCurlyBracket).Once()
                        .Then(ParserRuleType.TPropertyValueAssignments).Once()
                        .Then(TokenType.CloseCurlyBracket).Once())
                .Rule(
                    ParserRuleType.TPropertyValueAssignments,
                    o => o
                        .With(ParserRuleType.TPropertyValueAssignment).Once()
                        .Then(ParserRuleType.TPropertyValueSubsequentAssignment).ZeroOrMore())
                .Rule(
                    ParserRuleType.TPropertyValueAssignment,
                    o => o
                        .With(ParserRuleType.TPropertyValuePathElement).Once()
                        .Then(ParserRuleType.TPropertyValuePathSubsequentElement).ZeroOrMore()
                        .Then(TokenType.Colon).Once()
                        .Then(TokenType.Variable).Once())
                .Rule(
                    ParserRuleType.TPropertyValueSubsequentAssignment,
                    o => o
                        .With(TokenType.Comma).Once()
                        .Then(ParserRuleType.TPropertyValueAssignment).Once())
                .Rule(
                    ParserRuleType.TPropertyValuePathElement,
                    o => o.With(TokenType.Item).Once(),
                    o => o.With(TokenType.QuotedItem).Once(),
                    o => o.With(TokenType.Variable).Once())
                .Rule(
                    ParserRuleType.TPropertyValuePathSubsequentElement,
                    o => o.With(TokenType.Dot).Once(),
                    o => o.With(ParserRuleType.TPropertyValuePathElement).Once());
        }

        private static Parser<TokenType, ParserRuleType> ConfigureSource(
            this Parser<TokenType, ParserRuleType> parser)
        {
            return parser
                .Rule(
                    ParserRuleType.Source,
                    o => o
                        .With(ParserRuleType.SFirstNode).Once()
                        .Then(ParserRuleType.SSubsequentNode).ZeroOrMore())
                .Rule(
                    ParserRuleType.SFirstNode,
                    o => o.With(ParserRuleType.SNode).Once(),
                    o => o.With(ParserRuleType.SIndexedNode).Once())
                .Rule(
                    ParserRuleType.SSubsequentNode,
                    o => o
                        .With(TokenType.Dot).Once()
                        .Then(ParserRuleType.SNode).Once(),
                    o => o.With(ParserRuleType.SIndexedNode).Once())
                .Rule(
                    ParserRuleType.SNode,
                    o => o.With(TokenType.Item).Once(),
                    o => o.With(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.SIndexedNode,
                    o => o
                        .With(TokenType.OpenSquareBracket).Once()
                        .Then(ParserRuleType.SIndexedNodeContent).Once()
                        .Then(ParserRuleType.SIndexedNodeSubsequentContent).ZeroOrMore()
                        .Then(TokenType.CloseSquareBracket).Once())
                .Rule(
                    ParserRuleType.SIndexedNodeContent,
                    o => o
                        .With(TokenType.Variable).Once()
                        .Then(ParserRuleType.SPropertyValueCapture).AtMostOnce()
                        .Then(ParserRuleType.SFilter).AtMostOnce())
                .Rule(
                    ParserRuleType.SIndexedNodeSubsequentContent,
                    o => o
                        .With(TokenType.Comma).Once()
                        .Then(ParserRuleType.SIndexedNodeContent).Once())
                .Rule(
                    ParserRuleType.SPropertyValueCapture,
                    o => o
                        .With(TokenType.OpenRoundBracket).Once()
                        .Then(TokenType.Colon).Once()
                        .Then(ParserRuleType.SPropertyValuePath).AtMostOnce()
                        .Then(TokenType.CloseRoundBracket).Once())
                .Rule(
                    ParserRuleType.SPropertyValuePath,
                    o => o
                        .With(ParserRuleType.SPropertyValuePathElement).Once()
                        .Then(ParserRuleType.SPropertyValuePathSubsequentElement).ZeroOrMore())
                .Rule(
                    ParserRuleType.SPropertyValuePathElement,
                    o => o.With(TokenType.Item).Once(),
                    o => o.With(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.SPropertyValuePathSubsequentElement,
                    o => o
                        .With(TokenType.Dot).Once()
                        .Then(ParserRuleType.SPropertyValuePathElement).Once())
                .Rule(
                    ParserRuleType.SFilter,
                    o => o
                        .With(TokenType.OpenCurlyBracket).Once()
                        .Then(ParserRuleType.SFilterExpressionOr).Once()
                        .Then(TokenType.CloseCurlyBracket).Once(),
                    o => o
                        .With(TokenType.OpenCurlyBracket).Once()
                        .Then(ParserRuleType.SPropertyValuePath).Once()
                        .Then(TokenType.Colon).Once()
                        .Then(ParserRuleType.SFilterExpressionOr).Once()
                        .Then(TokenType.CloseCurlyBracket).Once())
                .Rule(
                    ParserRuleType.SFilterExpressionAnd,
                    o => o
                        .With(ParserRuleType.SFilterExpressionElement).Once()
                        .Then(ParserRuleType.SFilterLogicExpressionAndRHS).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.SFilterExpressionOr,
                    o => o
                        .With(ParserRuleType.SFilterExpressionAnd).Once()
                        .Then(ParserRuleType.SFilterLogicExpressionOrRHS).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.SFilterExpressionElement,
                    o => o.With(ParserRuleType.SFilterValue).Once(),
                    o => o
                        .With(TokenType.OpenRoundBracket).Once()
                        .Then(ParserRuleType.SFilterExpressionOr).Once()
                        .Then(TokenType.CloseRoundBracket).Once())
                .Rule(
                    ParserRuleType.SFilterLogicExpressionAndRHS,
                    o => o
                        .With(TokenType.And).Once()
                        .Then(ParserRuleType.SFilterExpressionElement).Once())
                .Rule(
                    ParserRuleType.SFilterLogicExpressionOrRHS,
                    o => o
                        .With(TokenType.Or).Once()
                        .Then(ParserRuleType.SFilterExpressionAnd).Once())
                .Rule(
                    ParserRuleType.SFilterValue,
                    o => o.With(ParserRuleType.SFilterItem).Hoist().Once(),
                    o => o.With(ParserRuleType.SFilterNegatedItem).Hoist().Once())
                .Rule(
                    ParserRuleType.SFilterItem,
                    o => o.With(ParserRuleType.SFilterItemElement).Hoist().AtLeastOnce(),
                    o => o.With(TokenType.QuotedItem).Once())
                .RuleMatchOneOf(
                    ParserRuleType.SFilterItemElement,
                    TokenType.Item,
                    TokenType.WildCard)
                .Rule(
                    ParserRuleType.SFilterNegatedItem,
                    o => o
                        .With(TokenType.ExclaimationMark).Once()
                        .Then(ParserRuleType.SFilterItem).Hoist().Once());
        }
    }
}