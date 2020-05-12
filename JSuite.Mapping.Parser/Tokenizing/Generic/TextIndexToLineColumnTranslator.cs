namespace JSuite.Mapping.Parser.Tokenizing.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TextIndexToLineColumnTranslator
    {
        private readonly IList<int> lineStartIndexes = new List<int>();

        public TextIndexToLineColumnTranslator(string text)
        {
            char? previousNewlineChar = null;
            this.lineStartIndexes.Add(0);
            for (int i = 0; i < text.Length; ++i)
            {
                var currentChar = text[i];
                if (currentChar == '\r' || currentChar == '\n')
                {
                    if (previousNewlineChar.HasValue)
                    {
                        if (previousNewlineChar != currentChar)
                        {
                            // This is either \r\n or \n\r, either of which are valid
                            // new lines so we should return the next index and reset.
                            // We ignore this if the new line index is outside of the
                            // text
                            if (i + 1 != text.Length)
                                this.lineStartIndexes.Add(i + 1);

                            previousNewlineChar = null;
                        }
                        else
                        {
                            // If this is the same as the previous then the previous
                            // was a new line in it's own right so return that and
                            // maintain tracking this char
                            this.lineStartIndexes.Add(i);
                        }
                    }
                    else
                    {
                        // This could be an independent new line or the start of a
                        // compound one so start tracking
                        previousNewlineChar = currentChar;
                    }
                }
                else if (previousNewlineChar.HasValue)
                {
                    // This is the start of the new line, return this
                    this.lineStartIndexes.Add(i);
                    previousNewlineChar = null;
                }
            }
        }

        public LineColumn Translate(int index)
        {
            for (int lineIndex = this.lineStartIndexes.Count - 1;
                lineIndex >= 0;
                --lineIndex)
            {
                var lineStartIndex = this.lineStartIndexes[lineIndex];
                if (lineStartIndex <= index)
                    return new LineColumn(lineIndex + 1, (index - lineStartIndex) + 1);
            }

            return default;
        }

        public IList<LineColumn> Translate(IEnumerable<int> indexes)
        {
            try
            {
                return indexes.Select(this.Translate).ToList();
            }
            catch (Exception)
            {
                /* Ignore exception since it is just trying to add context. */
                return null;
            }
        }
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
}