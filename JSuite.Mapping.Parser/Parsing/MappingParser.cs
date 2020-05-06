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
            = new Parser<TokenType, ParserRuleType>().Configure();

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

        private static Parser<TokenType, ParserRuleType> Configure(
            this Parser<TokenType, ParserRuleType> parser)
            => parser
                .Rule(
                    ParserRuleType.Mapping,
                    o => o
                        .With(ParserRuleType.Target).AtMostOnce()
                        .Then(TokenType.Equals).Exclude().Once()
                        .Then(ParserRuleType.Source).AtMostOnce())
                .ConfigureTarget()
                .ConfigureSource()
                .WithRoot(ParserRuleType.Mapping);

        private static Parser<TokenType, ParserRuleType> ConfigureTarget(
            this Parser<TokenType, ParserRuleType> parser)
        {
            return parser
                .Rule(
                    ParserRuleType.Target,
                    o => o
                        .With(ParserRuleType.TFirstNode).Hoist().Once()
                        .Then(ParserRuleType.TSubsequentNode).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.TFirstNode,
                    o => o.With(ParserRuleType.TNode).Once(),
                    o => o.With(ParserRuleType.TIndexedNode).Once())
                .Rule(
                    ParserRuleType.TSubsequentNode,
                    o => o
                        .With(TokenType.Dot).Exclude().Once()
                        .Then(ParserRuleType.TNode).Once(),
                    o => o.With(ParserRuleType.TIndexedNode).Once())
                .Rule(
                    ParserRuleType.TNode,
                    o => o
                        .With(ParserRuleType.TPathElement).Once()
                        .Then(ParserRuleType.TNodeModifiers).Hoist().Once())
                .Rule(
                    ParserRuleType.TPathElement,
                    o => o.With(TokenType.Item).Once(),
                    o => o.With(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.TIndexedNode,
                    o => o
                        .With(TokenType.OpenSquareBracket).Exclude().Once()
                        .Then(ParserRuleType.TIndexedNodeContent).ZeroOrMore()
                        .Then(TokenType.CloseSquareBracket).Exclude().Once()
                        .Then(ParserRuleType.TNodeModifiers).Hoist().Once())
                .RuleMatchNoneOf(
                    ParserRuleType.TIndexedNodeContent,
                    TokenType.CloseSquareBracket)
                .Rule(
                    ParserRuleType.TNodeModifiers,
                    o => o
                        .With(ParserRuleType.TPropertyValue).AtMostOnce()
                        .Then(ParserRuleType.TConditionalModifier).AtMostOnce())
                .RuleMatchOneOf(
                    ParserRuleType.TConditionalModifier,
                    TokenType.QuestionMark,
                    TokenType.ExclaimationMark)
                .Rule(
                    ParserRuleType.TPropertyValue,
                    o => o
                        .With(TokenType.OpenCurlyBracket).Exclude().Once()
                        .Then(ParserRuleType.TPropertyValueAssignment).Once()
                        .Then(ParserRuleType.TPropertyValueSubsequentAssignment).Hoist().ZeroOrMore()
                        .Then(TokenType.CloseCurlyBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.TPropertyValueAssignment,
                    o => o
                        .With(ParserRuleType.TPropertyValuePath).Once()
                        .Then(TokenType.Colon).Exclude().Once()
                        .Then(TokenType.Variable).Once())
                .Rule(
                    ParserRuleType.TPropertyValueSubsequentAssignment,
                    o => o
                        .With(TokenType.Comma).Exclude().Once()
                        .Then(ParserRuleType.TPropertyValueAssignment).Once())
                .Rule(
                    ParserRuleType.TPropertyValuePath,
                    o => o
                        .With(ParserRuleType.TPropertyValuePathElement).Hoist().Once()
                        .Then(ParserRuleType.TPropertyValuePathSubsequentElement).Hoist().ZeroOrMore())
                .RuleMatchOneOf(
                    ParserRuleType.TPropertyValuePathElement,
                    TokenType.Item,
                    TokenType.QuotedItem,
                    TokenType.Variable)
                .Rule(
                    ParserRuleType.TPropertyValuePathSubsequentElement,
                    o => o.With(TokenType.Dot).Exclude().Once(),
                    o => o.With(ParserRuleType.TPropertyValuePathElement).Hoist().Once());
        }

        private static Parser<TokenType, ParserRuleType> ConfigureSource(
            this Parser<TokenType, ParserRuleType> parser)
        {
            return parser
                .Rule(
                    ParserRuleType.Source,
                    o => o
                        .With(ParserRuleType.SFirstNode).Hoist().Once()
                        .Then(ParserRuleType.SSubsequentNode).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.SFirstNode,
                    o => o.With(ParserRuleType.SNode).Once(),
                    o => o.With(ParserRuleType.SIndexedNode).Once())
                .Rule(
                    ParserRuleType.SSubsequentNode,
                    o => o
                        .With(TokenType.Dot).Exclude().Once()
                        .Then(ParserRuleType.SNode).Once(),
                    o => o.With(ParserRuleType.SIndexedNode).Once())
                .Rule(
                    ParserRuleType.SNode,
                    o => o.With(TokenType.Item).Once(),
                    o => o.With(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.SIndexedNode,
                    o => o
                        .With(TokenType.OpenSquareBracket).Exclude().Once()
                        .Then(ParserRuleType.SIndexedNodeContent).Once()
                        .Then(ParserRuleType.SIndexedNodeSubsequentContent).Hoist().ZeroOrMore()
                        .Then(TokenType.CloseSquareBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.SIndexedNodeContent,
                    o => o
                        .With(TokenType.Variable).Once()
                        .Then(ParserRuleType.SPropertyValueCapture).AtMostOnce()
                        .Then(ParserRuleType.SFilter).AtMostOnce())
                .Rule(
                    ParserRuleType.SIndexedNodeSubsequentContent,
                    o => o
                        .With(TokenType.Comma).Exclude().Once()
                        .Then(ParserRuleType.SIndexedNodeContent).Once())
                .Rule(
                    ParserRuleType.SPropertyValueCapture,
                    o => o
                        .With(TokenType.OpenRoundBracket).Exclude().Once()
                        .Then(TokenType.Colon).Exclude().Once()
                        .Then(ParserRuleType.SPropertyValuePath).AtMostOnce()
                        .Then(TokenType.CloseRoundBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.SPropertyValuePath,
                    o => o
                        .With(ParserRuleType.SPropertyValuePathElement).Once()
                        .Then(ParserRuleType.SPropertyValuePathSubsequentElement).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.SPropertyValuePathElement,
                    o => o.With(TokenType.Item).Once(),
                    o => o.With(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.SPropertyValuePathSubsequentElement,
                    o => o
                        .With(TokenType.Dot).Exclude().Once()
                        .Then(ParserRuleType.SPropertyValuePathElement).Once())
                .Rule(
                    ParserRuleType.SFilter,
                    o => o
                        .With(TokenType.OpenCurlyBracket).Exclude().Once()
                        .Then(ParserRuleType.SFilterExpressionOr).Once()
                        .Then(TokenType.CloseCurlyBracket).Exclude().Once(),
                    o => o
                        .With(TokenType.OpenCurlyBracket).Exclude().Once()
                        .Then(ParserRuleType.SPropertyValuePath).Once()
                        .Then(TokenType.Colon).Exclude().Once()
                        .Then(ParserRuleType.SFilterExpressionOr).Once()
                        .Then(TokenType.CloseCurlyBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.SFilterExpressionAnd,
                    o => o
                        .With(ParserRuleType.SFilterExpressionElement).Hoist().Once()
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
                        .With(TokenType.OpenRoundBracket).Exclude().Once()
                        .Then(ParserRuleType.SFilterExpressionOr).Once()
                        .Then(TokenType.CloseRoundBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.SFilterLogicExpressionAndRHS,
                    o => o
                        .With(TokenType.And).Exclude().Once()
                        .Then(ParserRuleType.SFilterExpressionElement).Hoist().Once())
                .Rule(
                    ParserRuleType.SFilterLogicExpressionOrRHS,
                    o => o
                        .With(TokenType.Or).Exclude().Once()
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