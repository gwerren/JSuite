namespace JSuite.Mapping.Parser.Parsing
{
    using System;
    using System.Collections.Generic;

    public static class ParseTreePrinting
    {
        private const int IndentBy = 6;
        private const int IndentByWithBrackets = 2;

        public static void Print<TToken, TRule>(this IEnumerable<IParseTree<TToken, TRule>> trees, bool addBrackets)
        {
            foreach (var tree in trees)
            {
                Console.WriteLine(new string('-', 50));
                tree.Print(addBrackets, 0);
            }

            Console.WriteLine(new string('-', 50));
        }

        public static void Print<TToken, TRule>(this IParseTree<TToken, TRule> tree, bool addBrackets)
            => tree?.Print(addBrackets, 0);

        private static void Print<TToken, TRule>(
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
                if (addBrackets)
                {
                    Console.Write(new string('.', indent));
                    Console.WriteLine("(");
                }

                var rule = (IParseTreeRule<TToken, TRule>)tree;
                foreach (var element in rule.Elements)
                    element.Print(addBrackets, indent + (addBrackets ? IndentByWithBrackets : IndentBy));

                if (addBrackets)
                {
                    Console.Write(new string('.', indent));
                    Console.WriteLine(")");
                }
            }
        }
    }
}