// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Visitor to determine the nearest variadic op node (formula separation) that consumes each span
    /// </summary>
    internal class DetermineNearestFormulaSeparationForSpansVisitor : IdentityTexlVisitor
    {
        private readonly IEnumerator<Span> _spans;

        private bool _hasMoreSpans = false;

        private readonly Stack<TexlNode> _variadicOpNodeStack = new();

        private readonly Dictionary<Span, bool> _isNestedSpan = new();

        private readonly TexlNode _root;

        public DetermineNearestFormulaSeparationForSpansVisitor(IEnumerable<Span> spans, TexlNode root)
        {
            // spans are assumed to be ordered by the start
            _spans = spans.GetEnumerator();
            _hasMoreSpans = _spans.MoveNext();
            _root = root;
        }

        #region Overridden Visit* functions
        public override bool PreVisit(VariadicOpNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            if (node.Op == VariadicOp.Chain)
            {
                _variadicOpNodeStack.Push(node);
            }
            return true;
        }

        public override bool PreVisit(AsNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override bool PreVisit(BinaryOpNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override bool PreVisit(CallNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override bool PreVisit(DottedNameNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override bool PreVisit(ListNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override bool PreVisit(RecordNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override bool PreVisit(StrInterpNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override bool PreVisit(TableNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override bool PreVisit(UnaryOpNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
            return true;
        }

        public override void PostVisit(VariadicOpNode node)
        {

            if (_variadicOpNodeStack.Count > 0 && ReferenceEquals(_variadicOpNodeStack.Peek(), node))
            {
                // Consume any trailing tokens that are captured by this variadic op node
                var nodeSpan = node.GetSourceBasedSpan();
                while (_hasMoreSpans)
                {
                    if (_spans.Current.Start >= nodeSpan.Min && _spans.Current.Start < nodeSpan.Lim)
                    {
                        _isNestedSpan.Add(_spans.Current, !ReferenceEquals(node, _root));
                        _hasMoreSpans = _spans.MoveNext();
                    }
                    else
                    {
                        break;
                    }
                }
                _variadicOpNodeStack.Pop();
            }
        }

        public override void Visit(ErrorNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
        }

        public override void Visit(BlankNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
        }

        public override void Visit(BoolLitNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
        }

        public override void Visit(StrLitNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
        }

        public override void Visit(NumLitNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
        }

        public override void Visit(FirstNameNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
        }

        public override void Visit(ParentNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
        }

        public override void Visit(SelfNode node)
        {
            CaptureSpansThatAreCapturedByTopMostVariadicOpNode(node);
        }
        #endregion

        /// <summary>
        /// Before visiting the direct child of a bottom most VariadicOpNode (top most on the stack) and its descendants, 
        /// determine which spans from the left are consumed by the parent variadic op node
        /// </summary>
        /// <param name="mayBeDirectChildOfTopMostVariadicOpNode">Direct child of the bottom most VariadicOpNode (top most on the stack)</param>
        private void CaptureSpansThatAreCapturedByTopMostVariadicOpNode(TexlNode mayBeDirectChildOfTopMostVariadicOpNode)
        {
            if (_variadicOpNodeStack.Count > 0 && !ReferenceEquals(mayBeDirectChildOfTopMostVariadicOpNode.Parent, _variadicOpNodeStack.Peek()))
            {
                return;
            }

            var nodeSpan = mayBeDirectChildOfTopMostVariadicOpNode.GetSourceBasedSpan();
            var parentNodeSpan = _variadicOpNodeStack.Count == 0 ? null : mayBeDirectChildOfTopMostVariadicOpNode.Parent?.GetSourceBasedSpan();

            while (_hasMoreSpans && _spans.Current.Start <= nodeSpan.Min)
            {
                // empty stack indicates that there's no formula separation (root node is not variadic op node)
                // in this case, no variadic op node consumes the left most span
                if (parentNodeSpan == null)
                {
                    _hasMoreSpans = _spans.MoveNext();
                }
                // see if the current leftmost span is consumed by the parent variadic op node
                else if (_spans.Current.Start >= parentNodeSpan.Min && _spans.Current.Start < parentNodeSpan.Lim)
                {
                    // If the parent variadic op node consumes this leftmost span and it is not the root node, 
                    // then we are looking at the span that represents formula separated at depth > 1 (nested separation)
                    _isNestedSpan.Add(_spans.Current, !ReferenceEquals(mayBeDirectChildOfTopMostVariadicOpNode.Parent, _root));
                    _hasMoreSpans = _spans.MoveNext();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Given the spans for formulas that are separated by chaining operator across different depths, 
        /// filters out the spans that represent formulas separated at depths > 1 
        /// by merging them into the spans that represent formulas separated at the top most level
        /// <para>
        /// Note: Spans must be ordered by their start indices
        /// </para>
        /// </summary>
        /// <param name="separatedFormulaSpansIncludingNested">
        ///     Ordered Spans for formulas that are separated by chaining operator across different depths.
        ///     Spans must be ordered by their start indices
        /// </param>
        /// <param name="root">Root of the parse tree</param>
        /// <returns>Spans representing formulas  separated at the top most level. 
        ///          Spans for formulas separated at depth > 1 are merged into nearest spans that represent formulas separated at the top most level.
        /// </returns>
        public static IEnumerable<Span> GetSpansForFormulasSeparatedAtTopMostLevel(IEnumerable<Span> separatedFormulaSpansIncludingNested, TexlNode root)
        {
            var visitor = new DetermineNearestFormulaSeparationForSpansVisitor(separatedFormulaSpansIncludingNested, root);
            root.Accept(visitor);
            var topMostSeparatedFormulasSpans = new List<Span>();
            Span lastSpan = null;

            foreach (var span in separatedFormulaSpansIncludingNested)
            {
                if (visitor._isNestedSpan.TryGetValue(span, out var isNestedSpan) && isNestedSpan)
                {
                    // if the span is nested (represents formula that is separated at depth > 1)
                    // merge it with the last span as formulas that are separated at depth > 1
                    // are on the same path so they appear in the consecutively in the separatedFormulaSpansIncludingNested 
                    // assuming that separatedFormulaSpansIncludingNested is sorted by theit start indices
                    lastSpan = lastSpan == null ? span : new Span(lastSpan.Start, span.End);
                }
                else
                {
                    if (lastSpan != null)
                    {
                        topMostSeparatedFormulasSpans.Add(lastSpan);
                    }
                    lastSpan = span;
                }
            }

            if (lastSpan != null)
            {
                topMostSeparatedFormulasSpans.Add(lastSpan);
            }
            return topMostSeparatedFormulasSpans;
        }
    }
}
