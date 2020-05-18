namespace JSuite.Mapping.Parser.Parsing
{
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Exceptions;
    using JSuite.Mapping.Parser.Parsing.Generic;
    using JSuite.Mapping.Parser.Tokenizing;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public static class PostParserExtensions
    {
        public static IEnumerable<IParseTree<TokenType, ParserRuleType>> Validate(
            this IEnumerable<IParseTree<TokenType, ParserRuleType>> parseTrees,
            ITextIndexHelper translator)
        {
            foreach (var tree in parseTrees)
            {
                // Check for duplicate variable definitions in source, each
                // variable should be defined once only.
                var sourceVariables = GetVariables(tree, ParserRuleType.Source);

                var duplicateVariables = sourceVariables
                    .GroupBy(o => o.Value)
                    .Where(o => o.Count() != 1)
                    .ToList();

                if (duplicateVariables.Count != 0)
                {
                    throw DuplicateSourceVariablesException.For(
                        duplicateVariables.SelectMany(o => o.OrderBy(v => v.StartIndex).Skip(1)).ToList(),
                        translator);
                }

                // Check for undefined variable definitions in target (i.e. in target but not source)
                var targetVariables = GetVariables(tree, ParserRuleType.Source);
                var sourceVariableNames = sourceVariables.Select(o => o.Value).ToHashSet();
                var undefinedVariables = targetVariables.Where(o => !sourceVariableNames.Contains(o.Value)).ToList();
                if (undefinedVariables.Count != 0)
                    throw UndefinedVariablesException.For(undefinedVariables, translator);

                // Return the tree.
                yield return tree;
            }
        }

        private static IList<Token<TokenType>> GetVariables(
            IParseTree<TokenType, ParserRuleType> treeRoot,
            ParserRuleType mappingSide)
            => ((IParseTreeRule<TokenType, ParserRuleType>)treeRoot).Elements
                .Cast<IParseTreeRule<TokenType, ParserRuleType>>()
                .Where(o => o.RuleType == mappingSide)
                .TokenNodes()
                .Select(o => o.Token)
                .Where(o => o.Type == TokenType.Variable)
                .ToList();
    }
}