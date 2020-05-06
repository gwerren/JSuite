namespace JSuite.Mapping.Parser.Tokenizing.Generic
{
    public readonly struct Token<TType>
    {
        public Token(TType type, string value, int startIndex)
        {
            this.Type = type;
            this.Value = value;
            this.StartIndex = startIndex;
        }

        public TType Type { get; }

        public string Value { get; }

        public int StartIndex { get; }

        internal Token<TType> WithNewValue(string value)
            => new Token<TType>(this.Type, value, this.StartIndex);

        internal Token<TType> SubToken(int index, int length)
            => new Token<TType>(
                this.Type,
                this.Value.Substring(index, length),
                this.StartIndex + index);

        internal Token<TType> SubToken(int index, int length, TType type)
            => new Token<TType>(
                type,
                this.Value.Substring(index, length),
                this.StartIndex + index);
    }
}