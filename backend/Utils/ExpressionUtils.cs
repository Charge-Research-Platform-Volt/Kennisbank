using System.Linq.Expressions;

namespace KnowledgeBank.Utils;

/// <summary>
/// An ExpressionVisitor that replaces all occurrences of a specified expression with a new one.
/// Useful for parameter substitution in expression trees.
/// </summary>
public class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _oldValue;
    private readonly Expression _newValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplaceExpressionVisitor"/> class.
    /// </summary>
    /// <param name="oldValue">The expression to be replaced.</param>
    /// <param name="newValue">The expression to replace with.</param>
    public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue;
        _newValue = newValue;
    }
    
    /// <summary>
    /// Visits an expression and replaces it if it matches the target expression.
    /// </summary>
    /// <param name="node">The current expression node being visited.</param>
    /// <returns>The original node, the replacement node, or a recursively visited version.</returns>
    public override Expression? Visit(Expression? node)
    {
        if (node == null)
            return null;
        if (node == _oldValue)
            return _newValue;
        return base.Visit(node);
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
