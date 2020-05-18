namespace JSuite.Mapping.Parser.Tokenizing.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public interface ITextIndexHelper
    {
        LineColumn LinePosition(int index);
    }

    public readonly struct LineColumn
    {
        public LineColumn(int line, int column)
        {
            this.Line = line;
            this.Column = column;
        }

        public int Line { get; }

        public int Column { get; }
    }

    public static class TextIndexHelperExtensions
    {
        public static IList<LineColumn> LinePosition(this ITextIndexHelper helper, IEnumerable<int> indexes)
        {
            try
            {
                return indexes.Select(helper.LinePosition).ToList();
            }
            catch (Exception)
            {
                /* Ignore exception since it is just trying to add context. */
                return null;
            }
        }
    }
}