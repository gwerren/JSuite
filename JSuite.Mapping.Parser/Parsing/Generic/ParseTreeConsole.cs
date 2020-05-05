namespace JSuite.Mapping.Parser.Parsing.Generic
{
    using System;
    using System.Collections.Generic;

    public static class ParseTreeConsole
    {
        private const int IndentBy = 6;
        private const int IndentByWithBrackets = 2;

        public static void ToConsole<TToken, TRule>(this IEnumerable<IParseTree<TToken, TRule>> trees, bool addBrackets)
        {
            foreach (var tree in trees)
            {
                Console.WriteLine(new string('-', 50));
                tree.ToConsole(addBrackets, 0);
            }

            Console.WriteLine(new string('-', 50));
        }

        public static void ToConsole<TToken, TRule>(this IParseTree<TToken, TRule> tree, bool addBrackets)
            => tree?.ToConsole(addBrackets, 0);

        private static void ToConsole<TToken, TRule>(
            this IParseTree<TToken, TRule> tree,
            bool addBrackets,
            int indent)
        {
            if (tree is IParseTreeToken<TToken, TRule> token)
            {
                Console.Write(new string(' ', indent));
                Console.WriteLine(token.Token.Value);
            }
            else
            {
                var rule = (IParseTreeRule<TToken, TRule>)tree;
                if (addBrackets)
                {
                    Console.Write(new string('.', indent));
                    Console.Write("(            [");
                    Console.Write(rule.RuleType.ToString());
                    Console.WriteLine("]");
                }

                foreach (var element in rule.Elements)
                    element.ToConsole(addBrackets, indent + (addBrackets ? IndentByWithBrackets : IndentBy));

                if (addBrackets)
                {
                    Console.Write(new string('.', indent));
                    Console.Write(")            [");
                    Console.Write(rule.RuleType.ToString());
                    Console.WriteLine("]");
                }
            }
        }
    }
}