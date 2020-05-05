﻿namespace JSuite.Mapping.Parser.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Tokenizing;

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
            if (parseTree == null || parseTree.CapturedTokenCount != tokens.Count)
                throw new ApplicationException($"Failed to match rule '{this.rootType}'.");

            return parseTree;
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

            public IParserRuleItemConfigurator<TToken, TRule> With(TToken type)
            {
                var option = new RuleOption(this.ruleType);
                this.options.Add(option);
                return option.AddElement(type);
            }

            public IParserRuleItemConfigurator<TToken, TRule> With(TRule type)
            {
                var option = new RuleOption(this.ruleType);
                this.options.Add(option);
                return option.AddElement(type);
            }

            public IEnumerable<TRule> DirectlyDependsOn()
                => this.options.SelectMany(o => o.DirectlyDependsOn());

            public IParseTree<TToken, TRule> TryParse(
                IList<Token<TToken>> tokens,
                int matchFrom,
                IDictionary<TRule, RuleDefinition> rulesByType)
            {
                foreach (var option in this.options)
                {
                    var parseTree = option.TryParse(tokens, matchFrom, rulesByType);
                    if (parseTree != null)
                        return parseTree;
                }

                return null;
            }

            private class RuleOption : IParserRuleContinuationConfigurator<TToken, TRule>
            {
                private readonly TRule ruleType;
                private readonly IList<RuleElement> elements = new List<RuleElement>();

                public RuleOption(TRule ruleType) { this.ruleType = ruleType; }

                IParserRuleItemConfigurator<TToken, TRule>
                    IParserRuleContinuationConfigurator<TToken, TRule>.Then(TToken type)
                    => this.AddElement(type);

                IParserRuleItemConfigurator<TToken, TRule>
                    IParserRuleContinuationConfigurator<TToken, TRule>.Then(TRule type)
                    => this.AddElement(type);

                public IParserRuleItemConfigurator<TToken, TRule> AddElement(TToken type)
                {
                    var element = new RuleTokenElement(type, this);
                    this.elements.Add(element);
                    return element;
                }

                public IParserRuleItemConfigurator<TToken, TRule> AddElement(TRule type)
                {
                    var element = new RuleSubRuleElement(type, this);
                    this.elements.Add(element);
                    return element;
                }

                public IEnumerable<TRule> DirectlyDependsOn()
                    => this.elements
                        .OfType<RuleSubRuleElement>()
                        .Select(o => o.RuleType);

                public IParseTree<TToken, TRule> TryParse(
                    IList<Token<TToken>> tokens,
                    int matchFrom,
                    IDictionary<TRule, RuleDefinition> rulesByType)
                {
                    var elementTrees = new List<IParseTree<TToken, TRule>>();
                    foreach (var element in this.elements)
                    {
                        var parseTreeNodes = element.TryParse(tokens, matchFrom, rulesByType);
                        if (parseTreeNodes == null)
                            return null;

                        elementTrees.AddRange(parseTreeNodes);
                        matchFrom += parseTreeNodes.CapturedTokenCount();
                    }

                    return new ParseTreeRule(this.ruleType, elementTrees);
                }

                private abstract class RuleElement : IParserRuleItemConfigurator<TToken, TRule>
                {
                    protected static readonly IList<IParseTree<TToken, TRule>> Empty
                        = new IParseTree<TToken, TRule>[0];

                    private readonly RuleOption option;
                    private RuleElementOccurrence occurrence;

                    protected RuleElement(RuleOption option) => this.option = option;

                    protected bool Flatten { get; private set; }

                    protected bool AllowZero
                        => this.occurrence == RuleElementOccurrence.AtMostOnce
                            || this.occurrence == RuleElementOccurrence.ZeroOrMore;

                    protected bool AllowMany
                        => this.occurrence == RuleElementOccurrence.AtLeastOnce
                            || this.occurrence == RuleElementOccurrence.ZeroOrMore;

                    public abstract IList<IParseTree<TToken, TRule>> TryParse(
                        IList<Token<TToken>> tokens,
                        int matchFrom,
                        IDictionary<TRule, RuleDefinition> rulesByType);

                    IParserRuleItemConfigurator<TToken, TRule>
                        IParserRuleItemConfigurator<TToken, TRule>.Flatten()
                    {
                        this.Flatten = true;
                        return this;
                    }

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

                private class RuleTokenElement : RuleElement
                {
                    private readonly TToken tokenType;

                    public RuleTokenElement(TToken tokenType, RuleOption option)
                        : base(option)
                        => this.tokenType = tokenType;

                    public override IList<IParseTree<TToken, TRule>> TryParse(
                        IList<Token<TToken>> tokens,
                        int matchFrom,
                        IDictionary<TRule, RuleDefinition> rulesByType)
                    {
                        if (matchFrom >= tokens.Count)
                            return this.AllowZero ? Empty : null;

                        if (!tokens[matchFrom].Type.Equals(this.tokenType))
                            return this.AllowZero ? Empty : null;

                        var matches = new List<IParseTree<TToken, TRule>> { new ParseTreeToken(tokens[matchFrom]) };
                        if (this.AllowMany)
                        {
                            ++matchFrom;
                            while (matchFrom < tokens.Count && tokens[matchFrom].Type.Equals(this.tokenType))
                            {
                                matches.Add(new ParseTreeToken(tokens[matchFrom]));
                                ++matchFrom;
                            }
                        }

                        return matches;
                    }
                }

                private class RuleSubRuleElement : RuleElement
                {
                    public RuleSubRuleElement(TRule ruleType, RuleOption option)
                        : base(option)
                        => this.RuleType = ruleType;

                    public TRule RuleType { get; }

                    public override IList<IParseTree<TToken, TRule>> TryParse(
                        IList<Token<TToken>> tokens,
                        int matchFrom,
                        IDictionary<TRule, RuleDefinition> rulesByType)
                    {
                        if (matchFrom >= tokens.Count)
                            return this.AllowZero ? Empty : null;

                        var currentParseTree = rulesByType[this.RuleType]
                            .TryParse(tokens, matchFrom, rulesByType);

                        if (currentParseTree == null)
                            return this.AllowZero ? Empty : null;

                        var matches = new List<IParseTree<TToken, TRule>> { currentParseTree };
                        if (this.AllowMany)
                        {
                            matchFrom += currentParseTree.CapturedTokenCount;
                            while (matchFrom < tokens.Count)
                            {
                                currentParseTree = rulesByType[this.RuleType]
                                    .TryParse(tokens, matchFrom, rulesByType);

                                if (currentParseTree == null)
                                    break;

                                matches.Add(currentParseTree);
                                matchFrom += currentParseTree.CapturedTokenCount;
                            }
                        }

                        return this.Flatten
                            ? matches.Tokens().ToList<IParseTree<TToken, TRule>>()
                            : matches;
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
                this.CapturedTokenCount = elements.CapturedTokenCount();
            }

            public int CapturedTokenCount { get; }

            public TRule RuleType { get; }

            public IList<IParseTree<TToken, TRule>> Elements { get; }
        }

        private class ParseTreeToken : IParseTreeToken<TToken, TRule>
        {
            public ParseTreeToken(Token<TToken> token) { this.Token = token; }

            public int CapturedTokenCount { get; } = 1;

            public Token<TToken> Token { get; }
        }
    }
}