﻿namespace JSuite.Mapping.Parser.Executing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using JSuite.Mapping.Parser.Parsing;
    using JSuite.Mapping.Parser.Parsing.Generic;
    using JSuite.Mapping.Parser.Tokenizing;
    using JSuite.Mapping.Parser.Tokenizing.Generic;
    using Newtonsoft.Json.Linq;

    public delegate void UpdateTarget(JObject target, ISourceMatch source);

    public static class TargetUpdaterBuilder
    {
        public static UpdateTarget TargetUpdater(
            this IParseTree<TokenType, ParserRuleType> mapping)
        {
            var target = mapping.Rule(ParserRuleType.Target);
            if (target == null)
                return Merging.MergeObjects;

            NodeUpdaterBase updater = new FinalNodeUpdater(
                (IParseTreeRule<TokenType, ParserRuleType>)target.Elements.Last());

            for (int i = target.Elements.Count - 2; i >= 0; --i)
            {
                updater = new NodeUpdater(
                    (IParseTreeRule<TokenType, ParserRuleType>)target.Elements[i],
                    updater);
            }

            return new Updater(updater).Update;
        }

        private static UpdatedItem AsUpdated(this JToken item) => new UpdatedItem(item);
        
        private abstract class NodeUpdaterBase
        {
            private readonly IList<PropertyValueUpdaterBase> propertySetters;
            private readonly IPropertyNameProvider propertyName;
            private readonly Conditional when;

            private enum Conditional
            {
                Always,
                IfNotExists,
                IfExists
            }

            protected NodeUpdaterBase(IParseTreeRule<TokenType, ParserRuleType> node)
            {
                // Handle property name
                if (node.RuleType == ParserRuleType.TNode)
                {
                    // The property name is defined in the first element
                    this.propertyName = new KnownPropertyName(node.Elements[0].Token().Value);
                }
                else if (node.RuleType == ParserRuleType.TIndexedNode)
                {
                    // The property name is defined as a series of
                    // tokens where some or all are variables
                    this.propertyName = new IndexedPropertyName(
                        node.Elements
                            .OfType<IParseTreeRule<TokenType, ParserRuleType>>()
                            .Where(o => o.RuleType == ParserRuleType.TIndexedNodeContent)
                            .Select(o => o.Token())
                            .ToList());
                }

                // Find whether this should be an array node
                this.IsArray = node.Elements
                    .Any(
                        o => o is IParseTreeToken<TokenType, ParserRuleType> token
                            && token.Token.Type == TokenType.Array);

                // Handle property value assignment
                var assignments = node.Rule(ParserRuleType.TPropertyValue);
                if (assignments != null)
                {
                    this.propertySetters = new List<PropertyValueUpdaterBase>();
                    foreach (IParseTreeRule<TokenType, ParserRuleType> assignment in assignments.Elements)
                    {
                        var path = ((IParseTreeRule<TokenType, ParserRuleType>)assignment.Elements[0]).Elements;
                        PropertyValueUpdaterBase setter = new PropertyValueUpdater(
                            path.Last().Token(),
                            assignment.Elements[1].Token());

                        for (int i = path.Count - 2; i >= 0; --i)
                            setter = new PropertyValueNodeUpdater(path[i].Token(), setter);

                        this.propertySetters.Add(setter);
                    }
                }

                // Handle conditional modifier
                var conditionalModifier = node.Rule(ParserRuleType.TConditionalModifier);
                this.when = conditionalModifier == null
                    ? Conditional.Always
                    : conditionalModifier.Token().Type == TokenType.ExclaimationMark
                        ? Conditional.IfNotExists
                        : Conditional.IfExists;
            }

            protected bool IsArray { get; }

            public bool Update(JObject target, ISourceMatch source)
            {
                var updated = this.DoUpdate(target, source);
                if (updated.WasUpdated && this.propertySetters != null && updated.Item is JObject updatedObj)
                {
                    foreach (var setter in this.propertySetters)
                        setter.Set(updatedObj, source);
                }

                return updated.WasUpdated;
            }

            protected abstract UpdatedItem DoUpdate(JObject target, ISourceMatch source);

            protected string GetPropertyName(ISourceMatch source) => this.propertyName.Get(source);

            protected bool ShouldMap(bool targetExists)
                => this.when == Conditional.Always
                    || (targetExists && this.when == Conditional.IfExists)
                    || (!targetExists && this.when == Conditional.IfNotExists);

            private interface IPropertyNameProvider
            {
                string Get(ISourceMatch source);
            }

            private class KnownPropertyName : IPropertyNameProvider
            {
                private readonly string name;

                public KnownPropertyName(string name) => this.name = name;

                public string Get(ISourceMatch source) => this.name;
            }

            private class IndexedPropertyName : IPropertyNameProvider
            {
                private readonly IList<Token<TokenType>> tokens;

                public IndexedPropertyName(IList<Token<TokenType>> tokens) => this.tokens = tokens;

                public string Get(ISourceMatch source)
                {
                    var builder = new StringBuilder();
                    foreach (var token in this.tokens)
                    {
                        builder.Append(
                            token.Type == TokenType.Variable
                                ? source.VariableValues[token.Value]
                                : token.Value);
                    }

                    return builder.ToString();
                }
            }
        }

        private class NodeUpdater : NodeUpdaterBase
        {
            private readonly NodeUpdaterBase next;

            public NodeUpdater(
                IParseTreeRule<TokenType, ParserRuleType> node,
                NodeUpdaterBase next)
                : base(node) { this.next = next; }

            protected override UpdatedItem DoUpdate(JObject target, ISourceMatch source)
            {
                var propertyName = this.GetPropertyName(source);
                var targetProperty = target.Property(propertyName);
                if (!this.ShouldMap(targetProperty != null))
                    return UpdatedItem.Notupdated;

                if (!this.IsArray && targetProperty != null && targetProperty.Value is JObject targetObj)
                {
                    return this.next.Update(targetObj, source)
                        ? targetObj.AsUpdated()
                        : UpdatedItem.Notupdated;
                }

                // If the target does not have an object to update then create it
                // and try to map to it, if that succeeds then assign to the target
                // either as the object or into the array (if this is an array)
                targetObj = new JObject();
                if (this.next.Update(targetObj, source))
                {
                    if (!this.IsArray)
                    {
                        target[propertyName] = targetObj;
                    }
                    else
                    {
                        if (targetProperty == null || !(targetProperty.Value is JArray targetArr))
                        {
                            targetArr = new JArray();
                            target[propertyName] = targetArr;
                        }
                        
                        targetArr.Add(targetObj);
                    }

                    return targetObj.AsUpdated();
                }

                return UpdatedItem.Notupdated;
            }
        }

        private class FinalNodeUpdater : NodeUpdaterBase
        {
            public FinalNodeUpdater(
                IParseTreeRule<TokenType, ParserRuleType> node)
                : base(node) { }

            protected override UpdatedItem DoUpdate(JObject target, ISourceMatch source)
            {
                var propertyName = this.GetPropertyName(source);
                if (!this.ShouldMap(target.ContainsKey(propertyName)))
                    return UpdatedItem.Notupdated;

                if (this.IsArray)
                {
                    var targetProperty = target.Property(propertyName);
                    if (targetProperty == null || !(targetProperty.Value is JArray targetArray))
                    {
                        targetArray = new JArray();
                        target[propertyName] = targetArray;
                    }

                    var targetElement = source.Element.DeepClone();
                    targetArray.Add(targetElement);
                    return targetElement.AsUpdated();
                }
                
                return Merging.MergeToProperty(target, propertyName, source.Element).AsUpdated();
            }
        }

        private abstract class PropertyValueUpdaterBase
        {
            private readonly Token<TokenType> pathNode;

            protected PropertyValueUpdaterBase(Token<TokenType> pathNode) => this.pathNode = pathNode;

            protected string GetPropertyName(ISourceMatch source)
                => this.pathNode.Type == TokenType.Variable
                    ? source.VariableValues[this.pathNode.Value]
                    : this.pathNode.Value;

            public abstract void Set(JObject target, ISourceMatch source);
        }

        private class PropertyValueNodeUpdater : PropertyValueUpdaterBase
        {
            private readonly PropertyValueUpdaterBase next;

            public PropertyValueNodeUpdater(
                Token<TokenType> pathNode,
                PropertyValueUpdaterBase next)
                : base(pathNode) => this.next = next;

            public override void Set(JObject target, ISourceMatch source)
            {
                var propertyName = this.GetPropertyName(source);
                var targetProperty = target.Property(propertyName);
                if (targetProperty == null || !(targetProperty.Value is JObject targetObj))
                {
                    // If the target does not have an object to update then create it
                    targetObj = new JObject();
                    target[propertyName] = targetObj;
                }

                this.next.Set(targetObj, source);
            }
        }

        private class PropertyValueUpdater : PropertyValueUpdaterBase
        {
            private readonly string valueVariableName;

            public PropertyValueUpdater(
                Token<TokenType> pathNode,
                Token<TokenType> valueVariable)
                : base(pathNode) => this.valueVariableName = valueVariable.Value;

            public override void Set(JObject target, ISourceMatch source)
                => Merging.MergeToProperty(
                    target,
                    this.GetPropertyName(source),
                    source.VariableValues[this.valueVariableName]);
        }

        private class Updater
        {
            private readonly NodeUpdaterBase updater;

            public Updater(NodeUpdaterBase updater) => this.updater = updater;

            public void Update(JObject target, ISourceMatch source)
                => this.updater.Update(target, source);
        }

        private static class Merging
        {
            public static void MergeObjects(JObject target, ISourceMatch source)
            {
                if (!(source.Element is JObject sourceObj))
                    throw new ApplicationException("Only an object can be merged with an object.");

                MergeObjects(target, sourceObj);
            }

            public static JToken MergeToProperty(
                JObject targetObject,
                string targetPropertyName,
                JToken sourceValue)
            {
                var targetProperty = targetObject.Property(targetPropertyName);
                if (targetProperty != null)
                {
                    if (targetProperty.Value is JObject targetObj
                        && sourceValue is JObject sourceObj)
                    {
                        // If the property exists in the target and both target and
                        // source properties are objects then we need to merge them
                        MergeObjects(targetObj, sourceObj);
                        return targetObj;
                    }

                    if (targetProperty.Value is JArray targetArr
                        && sourceValue is JArray sourceArr)
                    {
                        // If the property exists in the target and both target and
                        // source properties are arrays then we need to merge them
                        foreach (var element in sourceArr)
                            targetArr.Add(element);
                        
                        return targetArr;
                    }
                }

                // If the property does not exist in the target or target and source are
                // not either both objects or arrays then simply copy source to target
                var updated = sourceValue.DeepClone();
                targetObject[targetPropertyName] = updated;
                return updated;
            }

            private static void MergeObjects(JObject target, JObject source)
            {
                foreach (var sourceProperty in source.Properties())
                    MergeToProperty(target, sourceProperty.Name, sourceProperty.Value);
            }
        }

        private readonly struct UpdatedItem
        {
            public UpdatedItem(JToken item)
            {
                this.WasUpdated = true;
                this.Item = item;
            }

            public bool WasUpdated { get; }

            public JToken Item { get; }

            public static UpdatedItem Notupdated => new UpdatedItem();
        }
    }
}