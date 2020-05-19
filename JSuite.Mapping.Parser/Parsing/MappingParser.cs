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

        public static IEnumerable<IParseTree<TokenType, ParserRuleType>> Parse(
            this IEnumerable<IList<Token<TokenType>>> tokens,
            ITextIndexHelper translator)
            => tokens.Select(o => Parser.Parse(o, translator));

        private static Parser<TokenType, TRule> RuleMatchNoneOf<TRule>(
            this Parser<TokenType, TRule> parser,
            TRule type,
            params TokenType[] tokens)
        {
            return parser.RuleMatchOneTokenOf(type, AllTokenTypes.Except(tokens).ToArray());
        }

        private static Parser<TokenType, ParserRuleType> Configure(
            this Parser<TokenType, ParserRuleType> parser)
            => parser
                .Rule(
                    ParserRuleType.Mapping,
                    o => o
                        .WithR(ParserRuleType.Target).AtMostOnce()
                        .ThenT(TokenType.Equals).Exclude().Once()
                        .ThenR(ParserRuleType.Source).AtMostOnce())
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
                        .WithR(ParserRuleType.TFirstNode).Hoist().Once()
                        .ThenR(ParserRuleType.TSubsequentNode).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.TFirstNode,
                    o => o.WithR(ParserRuleType.TNode).Once(),
                    o => o.WithR(ParserRuleType.TIndexedNode).Once())
                .Rule(
                    ParserRuleType.TSubsequentNode,
                    o => o
                        .WithT(TokenType.Dot).Exclude().Once()
                        .ThenR(ParserRuleType.TNode).Once(),
                    o => o.WithR(ParserRuleType.TIndexedNode).Once())
                .Rule(
                    ParserRuleType.TNode,
                    o => o
                        .WithR(ParserRuleType.TPathElement).Once()
                        .ThenT(TokenType.Array).AtMostOnce()
                        .ThenR(ParserRuleType.TNodeModifiers).Hoist().Once())
                .Rule(
                    ParserRuleType.TPathElement,
                    o => o.WithT(TokenType.Item).Once(),
                    o => o.WithT(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.TIndexedNode,
                    o => o
                        .WithT(TokenType.OpenSquareBracket).Exclude().Once()
                        .ThenR(ParserRuleType.TIndexedNodeContent).ZeroOrMore()
                        .ThenT(TokenType.CloseSquareBracket).Exclude().Once()
                        .ThenT(TokenType.Array).AtMostOnce()
                        .ThenR(ParserRuleType.TNodeModifiers).Hoist().Once())
                .RuleMatchNoneOf(
                    ParserRuleType.TIndexedNodeContent,
                    TokenType.CloseSquareBracket)
                .Rule(
                    ParserRuleType.TNodeModifiers,
                    o => o
                        .WithR(ParserRuleType.TPropertyValue).AtMostOnce()
                        .ThenR(ParserRuleType.TConditionalModifier).AtMostOnce())
                .RuleMatchOneTokenOf(
                    ParserRuleType.TConditionalModifier,
                    TokenType.QuestionMark,
                    TokenType.ExclaimationMark)
                .Rule(
                    ParserRuleType.TPropertyValue,
                    o => o
                        .WithT(TokenType.OpenCurlyBracket).Exclude().Once()
                        .ThenR(ParserRuleType.TPropertyValueAssignment).Once()
                        .ThenR(ParserRuleType.TPropertyValueSubsequentAssignment).Hoist().ZeroOrMore()
                        .ThenT(TokenType.CloseCurlyBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.TPropertyValueAssignment,
                    o => o
                        .WithR(ParserRuleType.TPropertyValuePath).Once()
                        .ThenT(TokenType.Colon).Exclude().Once()
                        .ThenT(TokenType.Variable).Once())
                .Rule(
                    ParserRuleType.TPropertyValueSubsequentAssignment,
                    o => o
                        .WithT(TokenType.Comma).Exclude().Once()
                        .ThenR(ParserRuleType.TPropertyValueAssignment).Once())
                .Rule(
                    ParserRuleType.TPropertyValuePath,
                    o => o
                        .WithR(ParserRuleType.TPropertyValuePathElement).Hoist().Once()
                        .ThenR(ParserRuleType.TPropertyValuePathSubsequentElement).Hoist().ZeroOrMore())
                .RuleMatchOneTokenOf(
                    ParserRuleType.TPropertyValuePathElement,
                    TokenType.Item,
                    TokenType.QuotedItem,
                    TokenType.Variable)
                .Rule(
                    ParserRuleType.TPropertyValuePathSubsequentElement,
                    o => o.WithT(TokenType.Dot).Exclude().Once(),
                    o => o.WithR(ParserRuleType.TPropertyValuePathElement).Hoist().Once());
        }

        private static Parser<TokenType, ParserRuleType> ConfigureSource(
            this Parser<TokenType, ParserRuleType> parser)
        {
            return parser
                .Rule(
                    ParserRuleType.Source,
                    o => o
                        .WithR(ParserRuleType.SFirstNode).Hoist().Once()
                        .ThenR(ParserRuleType.SSubsequentNode).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.SFirstNode,
                    o => o.WithR(ParserRuleType.SNode).Once(),
                    o => o.WithR(ParserRuleType.SIndexedNode).Once())
                .Rule(
                    ParserRuleType.SSubsequentNode,
                    o => o
                        .WithT(TokenType.Dot).Exclude().Once()
                        .ThenR(ParserRuleType.SNode).Once(),
                    o => o.WithR(ParserRuleType.SIndexedNode).Once())
                .Rule(
                    ParserRuleType.SNode,
                    o => o.WithT(TokenType.Item).Once(),
                    o => o.WithT(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.SIndexedNode,
                    o => o
                        .WithT(TokenType.OpenSquareBracket).Exclude().Once()
                        .ThenR(ParserRuleType.SIndexedNodeContent).Once()
                        .ThenR(ParserRuleType.SIndexedNodeSubsequentContent).Hoist().ZeroOrMore()
                        .ThenT(TokenType.CloseSquareBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.SIndexedNodeContent,
                    o => o
                        .WithT(TokenType.Variable).Once()
                        .ThenR(ParserRuleType.SPropertyValueCapture).AtMostOnce()
                        .ThenR(ParserRuleType.SFilter).AtMostOnce())
                .Rule(
                    ParserRuleType.SIndexedNodeSubsequentContent,
                    o => o
                        .WithT(TokenType.Comma).Exclude().Once()
                        .ThenR(ParserRuleType.SIndexedNodeContent).Once())
                .Rule(
                    ParserRuleType.SPropertyValueCapture,
                    o => o
                        .WithT(TokenType.OpenRoundBracket).Exclude().Once()
                        .ThenT(TokenType.Colon).Exclude().Once()
                        .ThenR(ParserRuleType.SPropertyValuePath).Hoist().AtMostOnce()
                        .ThenT(TokenType.CloseRoundBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.SPropertyValuePath,
                    o => o
                        .WithR(ParserRuleType.SPropertyValuePathElement).Once()
                        .ThenR(ParserRuleType.SPropertyValuePathSubsequentElement).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.SPropertyValuePathElement,
                    o => o.WithT(TokenType.Item).Once(),
                    o => o.WithT(TokenType.QuotedItem).Once())
                .Rule(
                    ParserRuleType.SPropertyValuePathSubsequentElement,
                    o => o
                        .WithT(TokenType.Dot).Exclude().Once()
                        .ThenR(ParserRuleType.SPropertyValuePathElement).Once())
                .Rule(
                    ParserRuleType.SFilter,
                    o => o
                        .WithT(TokenType.OpenCurlyBracket).Exclude().Once()
                        .ThenR(ParserRuleType.SFilterExpressionOr).Once()
                        .ThenT(TokenType.CloseCurlyBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.SFilterExpressionAnd,
                    o => o
                        .WithR(ParserRuleType.SFilterExpressionElement).Hoist().Once()
                        .ThenR(ParserRuleType.SFilterLogicExpressionAndRHS).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.SFilterExpressionOr,
                    o => o
                        .WithR(ParserRuleType.SFilterExpressionAnd).Once()
                        .ThenR(ParserRuleType.SFilterLogicExpressionOrRHS).Hoist().ZeroOrMore())
                .Rule(
                    ParserRuleType.SFilterExpressionElement,
                    o => o.WithR(ParserRuleType.SFilterValue).Once(),
                    o => o
                        .WithT(TokenType.OpenRoundBracket).Exclude().Once()
                        .ThenR(ParserRuleType.SFilterExpressionOr).Once()
                        .ThenT(TokenType.CloseRoundBracket).Exclude().Once())
                .Rule(
                    ParserRuleType.SFilterLogicExpressionAndRHS,
                    o => o
                        .WithT(TokenType.And).Exclude().Once()
                        .ThenR(ParserRuleType.SFilterExpressionElement).Hoist().Once())
                .Rule(
                    ParserRuleType.SFilterLogicExpressionOrRHS,
                    o => o
                        .WithT(TokenType.Or).Exclude().Once()
                        .ThenR(ParserRuleType.SFilterExpressionAnd).Once())
                .Rule(
                    ParserRuleType.SFilterValue,
                    o => o.WithR(ParserRuleType.SFilterItem).Hoist().Once(),
                    o => o.WithR(ParserRuleType.SFilterNegatedItem).Hoist().Once())
                .Rule(
                    ParserRuleType.SFilterItem,
                    o => o.WithR(ParserRuleType.SFilterItemElement).Hoist().AtLeastOnce(),
                    o => o.WithT(TokenType.QuotedItem).Once())
                .RuleMatchOneTokenOf(
                    ParserRuleType.SFilterItemElement,
                    TokenType.Item,
                    TokenType.WildCard)
                .Rule(
                    ParserRuleType.SFilterNegatedItem,
                    o => o
                        .WithT(TokenType.ExclaimationMark).Once()
                        .ThenR(ParserRuleType.SFilterItem).Hoist().Once());
        }
    }
}