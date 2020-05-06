namespace JSuite.Mapping.Parser.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JSuite.Mapping.Parser.Tokenizing;
    using JSuite.Mapping.Parser.Tokenizing.Generic;

    public static class PreParserExtensions
    {
        private static readonly IList<TokenType> IgnoredTokenTypes = new[]
        {
            TokenType.Whitespace,
            TokenType.Comment,
            TokenType.LineContinuation
        };

        public static IEnumerable<Token<TokenType>> ApplyModifications(
            this IEnumerable<Token<TokenType>> tokens)
        {
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.QuotedItem)
                {
                    yield return token.WithNewValue(
                        token.Value
                            .Substring(1, token.Value.Length - 2)
                            .Replace("\"\"", "\""));
                }
                else
                {
                    yield return token;
                }
            }
        }

        public static IEnumerable<IList<Token<TokenType>>> ToStatements(
            this IEnumerable<Token<TokenType>> tokens)
        {
            var currentStatement = new List<Token<TokenType>>();
            foreach (var token in tokens)
            {
                if (IgnoredTokenTypes.Contains(token.Type))
                    continue;

                // Ignore multiple new lines in a row (even if
                // seperated by other ignore items, e.g. a comment)
                if (token.Type == TokenType.NewLine)
                {
                    if (currentStatement.Count != 0)
                    {
                        yield return currentStatement;
                        currentStatement = new List<Token<TokenType>>();
                    }

                    continue;
                }

                currentStatement.Add(token);
            }

            if (currentStatement.Count != 0)
                yield return currentStatement;
        }

        public static IList<IList<Token<TokenType>>> ApplyPartials(
            this IEnumerable<IList<Token<TokenType>>> statements)
        {
            var outputStatements = new List<IList<Token<TokenType>>>();
            var partialDefinitionsByName = new Dictionary<string, IList<Token<TokenType>>>();
            var partialDependencyPartialsByDependent = new Dictionary<string, IList<string>>();
            var statementIndexesContainingPartials = new List<int>();

            // Seperate statements and partial definitions
            // and build partial dependency lists
            foreach (var statement in statements)
            {
                if (statement.Count >= 2
                    && statement[0].Type == TokenType.Partial
                    && statement[1].Type == TokenType.PartialAssignment)
                {
                    if (!partialDefinitionsByName
                        .TryAdd(statement[0].Value, statement.Skip(2).ToList()))
                    {
                        throw new ApplicationException(
                            $"The partial {statement[0].Value} is defined multiple times.");
                    }

                    var dependencies = statement
                        .Skip(2)
                        .Where(o => o.Type == TokenType.Partial)
                        .Select(o => o.Value)
                        .Distinct()
                        .ToList();

                    if (dependencies.Count != 0)
                    {
                        if (dependencies.Contains(statement[0].Value))
                        {
                            throw new ApplicationException(
                                $"Partial {statement[0].Value} depends on itself.");
                        }

                        partialDependencyPartialsByDependent.Add(
                            statement[0].Value,
                            dependencies);
                    }
                }
                else
                {
                    if (statement.Any(o => o.Type == TokenType.Partial))
                        statementIndexesContainingPartials.Add(outputStatements.Count);

                    outputStatements.Add(statement);
                }
            }

            // Replace partials in partials
            var previousPartialDependencyCount = partialDependencyPartialsByDependent.Count + 1;
            while (partialDependencyPartialsByDependent.Count != 0
                && partialDependencyPartialsByDependent.Count != previousPartialDependencyCount)
            {
                var canUpdate = partialDependencyPartialsByDependent
                    .Where(o => o.Value.All(v => !partialDependencyPartialsByDependent.ContainsKey(v)))
                    .ToList();

                foreach (var partial in canUpdate)
                {
                    partialDefinitionsByName[partial.Key]
                        = partialDefinitionsByName[partial.Key]
                            .ReplacePartials(partialDefinitionsByName)
                            .ToList();

                    partialDependencyPartialsByDependent.Remove(partial.Key);
                }
            }

            if (partialDependencyPartialsByDependent.Count != 0)
                throw new ApplicationException("Circular dependencies found in partial definitions.");

            // Replace partials in statements
            foreach (var statementIndex in statementIndexesContainingPartials)
            {
                outputStatements[statementIndex] = outputStatements[statementIndex]
                    .ReplacePartials(partialDefinitionsByName)
                    .ToList();
            }

            return outputStatements;
        }

        private static IEnumerable<Token<TokenType>> ReplacePartials(
            this IEnumerable<Token<TokenType>> tokens,
            IDictionary<string, IList<Token<TokenType>>> partialDefinitionsByName)
        {
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Partial)
                {
                    if (!partialDefinitionsByName.TryGetValue(token.Value, out var partial))
                        throw new ApplicationException($"No definition found for partial {token.Value}");

                    foreach (var partialToken in partial)
                        yield return partialToken;
                }
                else
                {
                    yield return token;
                }
            }
        }
    }
}