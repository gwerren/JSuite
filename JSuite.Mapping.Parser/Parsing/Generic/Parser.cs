namespace JSuite.Mapping.Parser.Parsing.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Exceptions;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public class Parser<TToken, TRule>
    {
        private readonly IDictionary<TRule, RuleDefinition> rulesByType
            = new Dictionary<TRule, RuleDefinition>();

        private TRule rootType;
        private bool configCompleted;

        public Parser<TToken, TRule> Rule(
            TRule type,
            params Action<IParserRuleConfigurator<TToken, TRule>>[] options)
        {
            if (this.configCompleted)
                throw ParserConfigurationException.AlreadyCompleted();

            if (!this.rulesByType.TryGetValue(type, out var rule))
            {
                rule = new RuleDefinition(type);
                this.rulesByType.Add(type, rule);
            }

            foreach (var option in options)
                option(rule);

            return this;
        }

        public Parser<TToken, TRule> RuleMatchOneOf(
            TRule type,
            params TToken[] tokens)
        {
            return this.Rule(
                type,
                tokens.Select(t => (Action<IParserRuleConfigurator<TToken, TRule>>)(c => c.With(t).Once())).ToArray());
        }

        public Parser<TToken, TRule> RuleMatchOneOf(
            TRule type,
            params TRule[] subRules)
        {
            return this.Rule(
                type,
                subRules.Select(s => (Action<IParserRuleConfigurator<TToken, TRule>>)(c => c.With(s).Once())).ToArray());
        }

        public Parser<TToken, TRule> WithRoot(TRule type)
        {
            if (this.configCompleted)
                throw ParserConfigurationException.AlreadyCompleted();

            this.rootType = type;
            this.configCompleted = true;

            // Validate the configuration to ensure
            // no expected rules are missing
            var validated = new HashSet<TRule>();
            var toValidate = new Stack<TRule>();
            toValidate.Push(type);
            while (toValidate.Count != 0)
            {
                var checking = toValidate.Pop();
                if (!this.rulesByType.TryGetValue(checking, out var checkingDefinition))
                    throw ParserConfigurationException.RuleNotDefined(checking.ToString());

                validated.Add(checking);
                foreach (var dependency in checkingDefinition
                    .DirectlyDependsOn()
                    .Where(o => !validated.Contains(o))
                    .Distinct())
                {
                    toValidate.Push(dependency);
                }
            }

            return this;
        }

        public IParseTree<TToken, TRule> Parse(IList<Token<TToken>> tokens)
        {
            if (!this.configCompleted)
                throw ParserConfigurationException.NotCompleted();

            var parseTree = this.rulesByType[this.rootType].TryParse(tokens, 0, this.rulesByType);
            if (parseTree.IsError || parseTree.NextTokenIndex != tokens.Count)
            {
                if (parseTree.MaxMatchedNextTokenIndex == tokens.Count)
                    throw new EndOfTokensException();

                if (parseTree.MaxMatchedNextTokenIndex < 0 || parseTree.MaxMatchedNextTokenIndex > tokens.Count)
                    throw new ApplicationException("An unexpected error was encountered.");

                throw UnexpectedTokenException.For(tokens[parseTree.MaxMatchedNextTokenIndex]);
            }

            return parseTree.TreeItems;
        }

        private enum RuleElementOccurrence
        {
            Once,
            AtMostOnce,
            AtLeastOnce,
            ZeroOrMore
        }

        private class RuleDefinition : IParserRuleConfigurator<TToken, TRule>
        {
            private readonly TRule ruleType;
            private readonly IList<RuleOption> options = new List<RuleOption>();

            public RuleDefinition(TRule ruleType) { this.ruleType = ruleType; }

            public IParserRuleTokenItemConfigurator<TToken, TRule> With(TToken type)
            {
                var option = new RuleOption(this.ruleType);
                this.options.Add(option);
                return option.AddElement(type);
            }

            public IParserRuleRuleItemConfigurator<TToken, TRule> With(TRule type)
            {
                var option = new RuleOption(this.ruleType);
                this.options.Add(option);
                return option.AddElement(type);
            }

            public IEnumerable<TRule> DirectlyDependsOn()
                => this.options.SelectMany(o => o.DirectlyDependsOn());

            public ParseTreeResult<IParseTree<TToken, TRule>> TryParse(
                IList<Token<TToken>> tokens,
                int matchFrom,
                IDictionary<TRule, RuleDefinition> rulesByType)
            {
                var maxNextToken = matchFrom;
                var maxMaxMatchedNextToken = matchFrom;
                foreach (var option in this.options)
                {
                    var parseTree = option.TryParse(tokens, matchFrom, rulesByType);
                    if (!parseTree.IsError)
                        return parseTree;

                    if (parseTree.NextTokenIndex > maxNextToken)
                        maxNextToken = parseTree.NextTokenIndex;

                    if (parseTree.MaxMatchedNextTokenIndex > maxMaxMatchedNextToken)
                        maxMaxMatchedNextToken = parseTree.MaxMatchedNextTokenIndex;
                }

                return RuleOptionResult.Error(maxNextToken, maxMaxMatchedNextToken);
            }

            private class RuleOption : IParserRuleContinuationConfigurator<TToken, TRule>
            {
                private readonly TRule ruleType;
                private readonly IList<RuleElement> elements = new List<RuleElement>();

                public RuleOption(TRule ruleType) { this.ruleType = ruleType; }

                IParserRuleTokenItemConfigurator<TToken, TRule>
                    IParserRuleContinuationConfigurator<TToken, TRule>.Then(TToken type)
                    => this.AddElement(type);

                IParserRuleRuleItemConfigurator<TToken, TRule>
                    IParserRuleContinuationConfigurator<TToken, TRule>.Then(TRule type)
                    => this.AddElement(type);

                public IParserRuleTokenItemConfigurator<TToken, TRule> AddElement(TToken type)
                {
                    var element = new RuleTokenElement(type, this);
                    this.elements.Add(element);
                    return element;
                }

                public IParserRuleRuleItemConfigurator<TToken, TRule> AddElement(TRule type)
                {
                    var element = new RuleSubRuleElement(type, this);
                    this.elements.Add(element);
                    return element;
                }

                public IEnumerable<TRule> DirectlyDependsOn()
                    => this.elements
                        .OfType<RuleSubRuleElement>()
                        .Select(o => o.RuleType);

                public ParseTreeResult<IParseTree<TToken, TRule>> TryParse(
                    IList<Token<TToken>> tokens,
                    int matchFrom,
                    IDictionary<TRule, RuleDefinition> rulesByType)
                {
                    var elementTrees = new List<IParseTree<TToken, TRule>>();
                    var maxMatchedNextTokenIndex = matchFrom;
                    foreach (var element in this.elements)
                    {
                        var parseTreeNodes = element.TryParse(tokens, matchFrom, rulesByType);
                        if (parseTreeNodes.IsError)
                        {
                            return RuleOptionResult.Error(
                                parseTreeNodes.NextTokenIndex,
                                Math.Max(parseTreeNodes.MaxMatchedNextTokenIndex, maxMatchedNextTokenIndex));
                        }

                        if (parseTreeNodes.HasTreeItems)
                            elementTrees.AddRange(parseTreeNodes.TreeItems);

                        matchFrom = parseTreeNodes.NextTokenIndex;
                        maxMatchedNextTokenIndex = parseTreeNodes.MaxMatchedNextTokenIndex;
                    }

                    return RuleOptionResult.NonEmpty(
                        new ParseTreeRule(this.ruleType, elementTrees),
                        matchFrom,
                        maxMatchedNextTokenIndex);
                }

                private abstract class RuleElement : IParserRuleItemConfigurator<TToken, TRule>
                {
                    private readonly RuleOption option;
                    private RuleElementOccurrence occurrence;

                    protected RuleElement(RuleOption option) => this.option = option;

                    protected bool AllowZero
                        => this.occurrence == RuleElementOccurrence.AtMostOnce
                            || this.occurrence == RuleElementOccurrence.ZeroOrMore;

                    protected bool AllowMany
                        => this.occurrence == RuleElementOccurrence.AtLeastOnce
                            || this.occurrence == RuleElementOccurrence.ZeroOrMore;

                    public abstract ParseTreeResult<IList<IParseTree<TToken, TRule>>> TryParse(
                        IList<Token<TToken>> tokens,
                        int matchFrom,
                        IDictionary<TRule, RuleDefinition> rulesByType);

                    IParserRuleContinuationConfigurator<TToken, TRule>
                        IParserRuleItemConfigurator<TToken, TRule>.Once()
                    {
                        this.occurrence = RuleElementOccurrence.Once;
                        return this.option;
                    }

                    IParserRuleContinuationConfigurator<TToken, TRule>
                        IParserRuleItemConfigurator<TToken, TRule>.AtLeastOnce()
                    {
                        this.occurrence = RuleElementOccurrence.AtLeastOnce;
                        return this.option;
                    }

                    IParserRuleContinuationConfigurator<TToken, TRule>
                        IParserRuleItemConfigurator<TToken, TRule>.AtMostOnce()
                    {
                        this.occurrence = RuleElementOccurrence.AtMostOnce;
                        return this.option;
                    }

                    IParserRuleContinuationConfigurator<TToken, TRule>
                        IParserRuleItemConfigurator<TToken, TRule>.ZeroOrMore()
                    {
                        this.occurrence = RuleElementOccurrence.ZeroOrMore;
                        return this.option;
                    }
                }

                private class RuleTokenElement : RuleElement, IParserRuleTokenItemConfigurator<TToken, TRule>
                {
                    private readonly TToken tokenType;
                    private bool exclude;

                    public RuleTokenElement(TToken tokenType, RuleOption option)
                        : base(option)
                        => this.tokenType = tokenType;

                    public IParserRuleTokenItemConfigurator<TToken, TRule> Exclude()
                    {
                        this.exclude = true;
                        return this;
                    }

                    public override ParseTreeResult<IList<IParseTree<TToken, TRule>>> TryParse(
                        IList<Token<TToken>> tokens,
                        int matchFrom,
                        IDictionary<TRule, RuleDefinition> rulesByType)
                    {
                        if (matchFrom >= tokens.Count)
                        {
                            return this.AllowZero
                                ? RuleElementResult.Empty(matchFrom, matchFrom)
                                : RuleElementResult.Error(matchFrom, matchFrom);
                        }

                        if (!tokens[matchFrom].Type.Equals(this.tokenType))
                        {
                            return this.AllowZero
                                ? RuleElementResult.Empty(matchFrom, matchFrom)
                                : RuleElementResult.Error(matchFrom, matchFrom);
                        }

                        var matches = this.exclude
                            ? null
                            : new List<IParseTree<TToken, TRule>> { new ParseTreeToken(tokens[matchFrom]) };

                        ++matchFrom;
                        if (this.AllowMany)
                        {
                            while (matchFrom < tokens.Count && tokens[matchFrom].Type.Equals(this.tokenType))
                            {
                                matches?.Add(new ParseTreeToken(tokens[matchFrom]));
                                ++matchFrom;
                            }
                        }

                        return RuleElementResult.NonEmpty(matches, matchFrom, matchFrom);
                    }
                }

                private class RuleSubRuleElement : RuleElement, IParserRuleRuleItemConfigurator<TToken, TRule>
                {
                    private bool hoist;

                    public RuleSubRuleElement(TRule ruleType, RuleOption option)
                        : base(option)
                        => this.RuleType = ruleType;

                    public TRule RuleType { get; }

                    IParserRuleRuleItemConfigurator<TToken, TRule>
                        IParserRuleRuleItemConfigurator<TToken, TRule>.Hoist()
                    {
                        this.hoist = true;
                        return this;
                    }

                    public override ParseTreeResult<IList<IParseTree<TToken, TRule>>> TryParse(
                        IList<Token<TToken>> tokens,
                        int matchFrom,
                        IDictionary<TRule, RuleDefinition> rulesByType)
                    {
                        if (matchFrom >= tokens.Count)
                        {
                            return this.AllowZero
                                ? RuleElementResult.Empty(matchFrom, matchFrom)
                                : RuleElementResult.Error(matchFrom, matchFrom);
                        }

                        var currentParseTree = rulesByType[this.RuleType]
                            .TryParse(tokens, matchFrom, rulesByType);

                        if (currentParseTree.IsError)
                        {
                            return this.AllowZero
                                ? RuleElementResult.Empty(matchFrom, currentParseTree.MaxMatchedNextTokenIndex)
                                : RuleElementResult.Error(currentParseTree.NextTokenIndex, currentParseTree.MaxMatchedNextTokenIndex);
                        }

                        var matches = new List<IParseTree<TToken, TRule>>();
                        if (currentParseTree.HasTreeItems)
                            matches.Add(currentParseTree.TreeItems);

                        matchFrom = currentParseTree.NextTokenIndex;
                        var maxMatchedNextTokenIndex = currentParseTree.MaxMatchedNextTokenIndex;
                        if (this.AllowMany)
                        {
                            while (matchFrom < tokens.Count)
                            {
                                currentParseTree = rulesByType[this.RuleType]
                                    .TryParse(tokens, matchFrom, rulesByType);

                                if (currentParseTree.IsError)
                                    break;

                                if (currentParseTree.HasTreeItems)
                                    matches.Add(currentParseTree.TreeItems);

                                matchFrom = currentParseTree.NextTokenIndex;
                                maxMatchedNextTokenIndex = currentParseTree.MaxMatchedNextTokenIndex;
                            }
                        }

                        return RuleElementResult.NonEmpty(
                            this.hoist ? matches.Elements().ToList() : matches,
                            matchFrom,
                            maxMatchedNextTokenIndex);
                    }
                }
            }
        }

        private class ParseTreeRule : IParseTreeRule<TToken, TRule>
        {
            public ParseTreeRule(TRule ruleType, IList<IParseTree<TToken, TRule>> elements)
            {
                this.RuleType = ruleType;
                this.Elements = elements;
            }

            public TRule RuleType { get; }

            public IList<IParseTree<TToken, TRule>> Elements { get; }
        }

        private class ParseTreeToken : IParseTreeToken<TToken, TRule>
        {
            public ParseTreeToken(Token<TToken> token) { this.Token = token; }

            public Token<TToken> Token { get; }
        }

        private static class RuleElementResult
        {
            public static ParseTreeResult<IList<IParseTree<TToken, TRule>>> Empty(
                int nextTokenIndex,
                int maxMatchedNextTokenIndex)
                => new ParseTreeResult<IList<IParseTree<TToken, TRule>>>(
                    false,
                    nextTokenIndex,
                    maxMatchedNextTokenIndex,
                    null);

            public static ParseTreeResult<IList<IParseTree<TToken, TRule>>> NonEmpty(
                IList<IParseTree<TToken, TRule>> treeItems,
                int nextTokenIndex,
                int maxMatchedNextTokenIndex)
                => new ParseTreeResult<IList<IParseTree<TToken, TRule>>>(
                    false,
                    nextTokenIndex,
                    maxMatchedNextTokenIndex,
                    treeItems);

            public static ParseTreeResult<IList<IParseTree<TToken, TRule>>> Error(
                int nextTokenIndex,
                int maxMatchedNextTokenIndex)
                => new ParseTreeResult<IList<IParseTree<TToken, TRule>>>(true, nextTokenIndex, maxMatchedNextTokenIndex, null);
        }

        private static class RuleOptionResult
        {
            public static ParseTreeResult<IParseTree<TToken, TRule>> NonEmpty(
                IParseTree<TToken, TRule> treeItem,
                int nextTokenIndex,
                int maxMatchedNextTokenIndex)
                => new ParseTreeResult<IParseTree<TToken, TRule>>(
                    false,
                    nextTokenIndex,
                    maxMatchedNextTokenIndex,
                    treeItem);

            public static ParseTreeResult<IParseTree<TToken, TRule>> Error(
                int nextTokenIndex,
                int maxMatchedNextTokenIndex)
                => new ParseTreeResult<IParseTree<TToken, TRule>>(true, nextTokenIndex, maxMatchedNextTokenIndex, null);
        }

        private readonly struct ParseTreeResult<T>
            where T: class
        {
            public ParseTreeResult(
                bool isError,
                int nextTokenIndex,
                int maxMatchedNextTokenIndex,
                T treeItems)
            {
                this.IsError = isError;
                this.HasTreeItems = treeItems != null;
                this.NextTokenIndex = nextTokenIndex;
                this.MaxMatchedNextTokenIndex = maxMatchedNextTokenIndex;
                this.TreeItems = treeItems;
            }

            public bool IsError { get; }

            public bool HasTreeItems { get; }

            public int NextTokenIndex { get; }

            public int MaxMatchedNextTokenIndex { get; }

            public T TreeItems { get; }
        }
    }
}