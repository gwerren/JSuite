namespace JSuite.Mapping.Parser.Parsing.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                throw new ApplicationException("Configuration has been completed, no changes can be made.");

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
                throw new ApplicationException("Configuration has been completed, no changes can be made.");

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
                    throw new ApplicationException($"The required rule '{checking}' has not been defined.");

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
                throw new ApplicationException("Configuration has not been completed.");

            var parseTree = this.rulesByType[this.rootType].TryParse(tokens, 0, this.rulesByType);
            if (!parseTree.MatchFound || parseTree.MatchedTokenCount != tokens.Count)
                throw new ApplicationException($"Failed to match rule '{this.rootType}'.");

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
                foreach (var option in this.options)
                {
                    var parseTree = option.TryParse(tokens, matchFrom, rulesByType);
                    if (parseTree.MatchFound)
                        return parseTree;
                }

                return default;
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
                    var matchedTokenCount = 0;
                    foreach (var element in this.elements)
                    {
                        var parseTreeNodes = element.TryParse(tokens, matchFrom, rulesByType);
                        if (!parseTreeNodes.MatchFound)
                            return default;

                        if (parseTreeNodes.HasTreeItems)
                            elementTrees.AddRange(parseTreeNodes.TreeItems);

                        matchFrom += parseTreeNodes.MatchedTokenCount;
                        matchedTokenCount += parseTreeNodes.MatchedTokenCount;
                    }

                    return new ParseTreeResult<IParseTree<TToken, TRule>>(
                        matchedTokenCount,
                        new ParseTreeRule(this.ruleType, elementTrees));
                }

                private abstract class RuleElement : IParserRuleItemConfigurator<TToken, TRule>
                {
                    protected static readonly ParseTreeResult<IList<IParseTree<TToken, TRule>>> Empty
                        = new ParseTreeResult<IList<IParseTree<TToken, TRule>>>(0);

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
                            return this.AllowZero ? Empty : default;

                        if (!tokens[matchFrom].Type.Equals(this.tokenType))
                            return this.AllowZero ? Empty : default;

                        var matches = this.exclude
                            ? null
                            : new List<IParseTree<TToken, TRule>> { new ParseTreeToken(tokens[matchFrom]) };

                        var matchedTokenCount = 1;
                        if (this.AllowMany)
                        {
                            ++matchFrom;
                            while (matchFrom < tokens.Count && tokens[matchFrom].Type.Equals(this.tokenType))
                            {
                                matches?.Add(new ParseTreeToken(tokens[matchFrom]));
                                ++matchFrom;
                                ++matchedTokenCount;
                            }
                        }

                        return new ParseTreeResult<IList<IParseTree<TToken, TRule>>>(matchedTokenCount, matches);
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
                            return this.AllowZero ? Empty : default;

                        var currentParseTree = rulesByType[this.RuleType]
                            .TryParse(tokens, matchFrom, rulesByType);

                        if (!currentParseTree.MatchFound)
                            return this.AllowZero ? Empty : default;

                        var matches = new List<IParseTree<TToken, TRule>>();
                        if (currentParseTree.HasTreeItems)
                            matches.Add(currentParseTree.TreeItems);

                        var matchedTokenCount = currentParseTree.MatchedTokenCount;
                        if (this.AllowMany)
                        {
                            matchFrom += currentParseTree.MatchedTokenCount;
                            while (matchFrom < tokens.Count)
                            {
                                currentParseTree = rulesByType[this.RuleType]
                                    .TryParse(tokens, matchFrom, rulesByType);

                                if (!currentParseTree.MatchFound)
                                    break;

                                if (currentParseTree.HasTreeItems)
                                    matches.Add(currentParseTree.TreeItems);

                                matchedTokenCount += currentParseTree.MatchedTokenCount;
                                matchFrom += currentParseTree.MatchedTokenCount;
                            }
                        }

                        return new ParseTreeResult<IList<IParseTree<TToken, TRule>>>(
                            matchedTokenCount,
                            this.hoist ? matches.Elements().ToList() : matches);
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

        private readonly struct ParseTreeResult<T>
            where T: class
        {
            public ParseTreeResult(int matchedTokenCount, T treeItems = null)
            {
                this.MatchFound = true;
                this.HasTreeItems = treeItems != null && matchedTokenCount != 0;
                this.MatchedTokenCount = matchedTokenCount;
                this.TreeItems = treeItems;
            }

            public bool MatchFound { get; }

            public bool HasTreeItems { get; }

            public int MatchedTokenCount { get; }

            public T TreeItems { get; }
        }
    }
}