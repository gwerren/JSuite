﻿namespace JSuite.Mapping.Parser.Parsing
{
    public enum ParserRuleType
    {
        Mapping,
        Target,
        TFirstNode,
        TSubsequentNode,
        TNode,
        TPathElement,
        TIndexedNode,
        TIndexedNodeContent,
        TNodeModifiers,
        TConditionalModifier,
        TPropertyValue,
        TPropertyValueAssignments,
        TPropertyValueAssignment,
        TPropertyValueSubsequentAssignment,
        TPropertyValuePathElement,
        TPropertyValuePathSubsequentElement,
        Source,
        SFirstNode,
        SSubsequentNode,
        SNode,
        SIndexedNode,
        SIndexedNodeContent,
        SIndexedNodeSubsequentContent,
        SPropertyValueCapture,
        SPropertyValuePath,
        SPropertyValuePathElement,
        SPropertyValuePathSubsequentElement,
        SFilter,
        SFilterExpressionOr,
        SFilterExpressionAnd,
        SFilterExpressionElement,
        SFilterLogicExpressionAndRHS,
        SFilterLogicExpressionOrRHS,
        SFilterValue,
        SFilterItem,
        SFilterItemElement,
        SFilterNegatedItem,
    }
}