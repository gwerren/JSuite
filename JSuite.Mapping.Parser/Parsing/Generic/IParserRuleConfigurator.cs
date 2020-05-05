namespace JSuite.Mapping.Parser.Parsing.Generic
{
    public interface IParserRuleConfigurator<TToken, TRule>
    {
        IParserRuleItemConfigurator<TToken, TRule> With(TToken tokenType);

        IParserRuleItemConfigurator<TToken, TRule> With(TRule ruleType);
    }

    public interface IParserRuleContinuationConfigurator<TToken, TRule>
    {
        IParserRuleItemConfigurator<TToken, TRule> Then(TToken tokenType);

        IParserRuleItemConfigurator<TToken, TRule> Then(TRule ruleType);
    }

    public interface IParserRuleItemConfigurator<TToken, TRule>
    {
        IParserRuleItemConfigurator<TToken, TRule> Hoist();
        IParserRuleContinuationConfigurator<TToken, TRule> Once();
        IParserRuleContinuationConfigurator<TToken, TRule> AtMostOnce();
        IParserRuleContinuationConfigurator<TToken, TRule> AtLeastOnce();
        IParserRuleContinuationConfigurator<TToken, TRule> ZeroOrMore();
    }
}