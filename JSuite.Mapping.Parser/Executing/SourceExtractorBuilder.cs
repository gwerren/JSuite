namespace JSuite.Mapping.Parser.Executing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Parsing;
    using JSuite.Mapping.Parser.Parsing.Generic;
    using JSuite.Mapping.Parser.Tokenizing;
    using Newtonsoft.Json.Linq;

    public interface ISourceMatch
    {
        JToken Element { get; }

        IReadOnlyDictionary<string, string> VariableValues { get; }
    }

    public delegate IEnumerable<ISourceMatch> ExtractFromSource(JObject sourceObject);

	public static class SourceExtractorBuilder
    {
        public static ExtractFromSource SourceExtractor(
            this IParseTree<TokenType, ParserRuleType> mapping)
        {
            var source = mapping.Rule(ParserRuleType.Source);
            if (source == null)
            {
                // If source is not defined we want the entire source object
                return o => new[] { new SourceMatch(o) };
            }

            return new Extractor(
                source.Elements
                    .Cast<IParseTreeRule<TokenType, ParserRuleType>>()
                    .Select(o => (Func<SourceMatch, IEnumerable<SourceMatch>>)MapNode(o).Extract)
                    .ToList())
                .Extract;
        }

        private static INodeExtractor MapNode(
            IParseTreeRule<TokenType, ParserRuleType> node)
        {
            switch (node.RuleType)
            {
                case ParserRuleType.SNode:
                    return new SNode(node);
                case ParserRuleType.SIndexedNode:
                    return new SIndexedNode(node);
                default:
                    throw new ApplicationException($"Source node type {node.RuleType} is not supported.");
            }
        }

        private interface INodeExtractor
        {
            IEnumerable<SourceMatch> Extract(SourceMatch extractedSoFar);
        }

        private class SNode : INodeExtractor
        {
            private readonly string nodeName;

            public SNode(IParseTreeRule<TokenType, ParserRuleType> node) => this.nodeName = node.Token().Value;

            public IEnumerable<SourceMatch> Extract(SourceMatch extractedSoFar)
            {
                if (extractedSoFar.Element is JObject obj
                    && obj.TryGetValue(this.nodeName, out var nested))
                {
                    yield return extractedSoFar.With(nested);
                }
            }
        }

        private class SIndexedNode : INodeExtractor
        {
            private readonly IList<VariableHandler> variableHandlers;

            public SIndexedNode(IParseTreeRule<TokenType, ParserRuleType> node)
            {
                this.variableHandlers = node.Elements
                    .Cast<IParseTreeRule<TokenType, ParserRuleType>>()
                    .Select(o => new VariableHandler(o))
                    .ToList();
            }

            public IEnumerable<SourceMatch> Extract(SourceMatch extractedSoFar)
            {
                int index = 0;
                foreach (var child in extractedSoFar.Element.Children())
                {
                    var extracted = extractedSoFar
                        .With(child is JProperty prop ? prop.Value : child);

                    var isValid = true;
                    foreach (var variable in this.variableHandlers)
                    {
                        if (!variable.TrySetValue(extracted, child, index))
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                        yield return extracted;

                    ++index;
                }
            }

            private class VariableHandler
            {
                private readonly string variableName;
                private readonly Func<JToken, int, string> variableCapture;
                private readonly IList<string> valueCapturePath;
                private readonly IFilter filter;

                public VariableHandler(IParseTreeRule<TokenType, ParserRuleType> indexedNode)
                {
                    this.variableName = indexedNode.Elements[0].Token().Value;

                    var valueCapture = indexedNode.Rule(ParserRuleType.SPropertyValueCapture);
                    if (valueCapture != null)
                    {
                        this.valueCapturePath = valueCapture.Elements.Select(o => o.Token().Value).ToList();
                        this.variableCapture = this.ValueCapture;
                    }
                    else
                    {
                        this.variableCapture = NameCapture;
                    }

                    var filterNode = indexedNode.Rule(ParserRuleType.SFilter);
                    if (filterNode != null)
                    {
                        this.filter = BuildFilter(
                            (IParseTreeRule<TokenType, ParserRuleType>)filterNode.Elements.Single());
                    }
                }

                public bool TrySetValue(SourceMatch extracted, JToken value, int index)
                {
                    var variableValue = this.variableCapture(value, index);
                    if (variableValue == null)
                        return false;

                    if (this.filter?.Include(variableValue) == false)
                        return false;

                    extracted.SetVariable(this.variableName, variableValue);
                    return true;
                }

                private static string NameCapture(JToken value, int index)
                    => value is JProperty property ? property.Name : index.ToString();

                private string ValueCapture(JToken value, int index)
                {
                    // Get the properties value if this is a property
                    if (value is JProperty prop)
                        value = prop.Value;

                    // Drill down the path to get the target value
                    foreach (var pathElement in this.valueCapturePath)
                    {
                        if (value is JObject obj)
                        {
                            if (!obj.TryGetValue(pathElement, out var next))
                                return null;

                            value = next;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    // If we found the value then return it
                    return value is JValue ? value.ToString() : null;
                }

                private static IFilter BuildFilter(IParseTreeRule<TokenType, ParserRuleType> filterElementNode)
                {
                    if (filterElementNode.RuleType == ParserRuleType.SFilterValue)
                        return new SFilterValue(filterElementNode);

                    var subFilters = filterElementNode.Elements
                        .Cast<IParseTreeRule<TokenType, ParserRuleType>>()
                        .Select(BuildFilter);

                    if (filterElementNode.RuleType == ParserRuleType.SFilterExpressionAnd)
                        return new SFilterExpressionAnd(subFilters);

                    if (filterElementNode.RuleType == ParserRuleType.SFilterExpressionOr)
                        return new SFilterExpressionOr(subFilters);

                    throw new ApplicationException(
                        $"The node type '{filterElementNode.RuleType}' was not expected in a filter.");
                }

                private interface IFilter
                {
                    bool Include(string value);
                }

                private class SFilterExpressionOr : IFilter
                {
                    private readonly IList<IFilter> elements;

                    public SFilterExpressionOr(IEnumerable<IFilter> elements) => this.elements = elements.ToList();

                    public bool Include(string value) => this.elements.Any(o => o.Include(value));
                }

                private class SFilterExpressionAnd : IFilter
                {
                    private readonly IList<IFilter> elements;

                    public SFilterExpressionAnd(IEnumerable<IFilter> elements) => this.elements = elements.ToList();

                    public bool Include(string value) => this.elements.All(o => o.Include(value));
                }

                private class SFilterValue : IFilter
                {
                    private readonly bool negate;
                    private readonly bool exactStart;
                    private readonly bool exactEnd;
                    private readonly IList<string> matchParts;

                    public SFilterValue(IParseTreeRule<TokenType, ParserRuleType> filterValueNode)
                    {
                        var tokens = filterValueNode.Tokens().ToList();
                        this.negate = tokens[0].Type == TokenType.ExclaimationMark;
                        this.exactStart = tokens[this.negate ? 1 : 0].Type != TokenType.WildCard;
                        this.exactEnd = tokens.Last().Type != TokenType.WildCard;

                        this.matchParts = tokens
                            .Skip(this.negate ? 1 : 0)
                            .Where(o => o.Type != TokenType.WildCard)
                            .Select(o => o.Value)
                            .ToList();
                    }

                    public bool Include(string value) => this.negate ? !this.IsMatch(value) : this.IsMatch(value);

                    private bool IsMatch(string value)
                    {
                        if (this.exactStart && !value.StartsWith(this.matchParts[0]))
                            return false;

                        var nextSearchIndex = this.exactStart ? this.matchParts[0].Length : 0;
                        for (int i = this.exactStart ? 1 : 0; i < this.matchParts.Count; ++i)
                        {
                            var matchPart = this.matchParts[i];
                            var index = value.IndexOf(matchPart, nextSearchIndex, StringComparison.Ordinal);
                            if (index < 0)
                                return false;

                            nextSearchIndex = index + matchPart.Length;
                        }

                        return !this.exactEnd || nextSearchIndex == value.Length;
                    }
                }
            }
        }

        private class Extractor
        {
            private readonly IList<Func<SourceMatch, IEnumerable<SourceMatch>>> extractors;

            public Extractor(IList<Func<SourceMatch, IEnumerable<SourceMatch>>> extractors)
            {
                this.extractors = extractors;
            }

            public IEnumerable<ISourceMatch> Extract(JObject source)
            {
                IEnumerable<SourceMatch> extractedSoFar = new[] { new SourceMatch(source) };
                foreach (var extractor in this.extractors)
                    extractedSoFar = DoExtract(extractor, extractedSoFar);

                return extractedSoFar;
            }

            private static IEnumerable<SourceMatch> DoExtract(
                Func<SourceMatch, IEnumerable<SourceMatch>> extractor,
                IEnumerable<SourceMatch> extractedSoFar)
            {
                foreach (var match in extractedSoFar)
                {
                    foreach (var extracted in extractor(match))
                        yield return extracted;
                }
            }
        }

        private class SourceMatch : ISourceMatch
        {
            private readonly Dictionary<string, string> variableValues;

            public SourceMatch(JToken element) : this(element, new Dictionary<string, string>()) { }

            private SourceMatch(JToken element, Dictionary<string, string> variableValues)
            {
                this.Element = element;
                this.variableValues = variableValues;
            }

            public JToken Element { get; }

            public IReadOnlyDictionary<string, string> VariableValues => this.variableValues;

            public SourceMatch With(JToken element)
                => new SourceMatch(element, new Dictionary<string, string>(this.variableValues));

            public void SetVariable(string name, string value) => this.variableValues[name] = value;
        }
    }
}