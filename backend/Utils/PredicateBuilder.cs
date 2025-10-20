using System.Linq.Expressions;

namespace KnowledgeBank.Utils;

public static class PredicateBuilder
{
    /// <summary>
    /// Combines two predicates using a logical OR.
    /// </summary>
    /// <returns>
    /// A predicate expression that evaluates to <c>true</c> if one of the predicates evaluate to <c>true</c>.
    /// </returns>
    public static Expression<Func<T, bool>> AddOr<T>(
        Expression<Func<T, bool>>? existingPredicate,
        Expression<Func<T, bool>> newPredicate)
    {
        return Combine(existingPredicate, newPredicate, Expression.OrElse);
    }

    /// <summary>
    /// Combines two predicates using a logical OR.
    /// </summary>
    /// <returns>
    /// A predicate expression that evaluates to <c>true</c> only if both predicates evaluate to <c>true</c>.
    /// </returns>
    public static Expression<Func<T, bool>> AddAnd<T>(
        Expression<Func<T, bool>>? existingPredicate,
        Expression<Func<T, bool>> newPredicate)
    {
        return Combine(existingPredicate, newPredicate, Expression.AndAlso);
    }

    /// <summary>
    /// Combines two predicate expressions of type <typeparamref name="T"/> into a single expression using a specified logical binary operator (e.g., AND, OR).
    /// If an existing predicate is provided, the new predicate is merged with it using the given combining function. 
    /// Otherwise, the new predicate is returned as-is.
    /// </summary>
    /// <typeparam name="T">The type of the entity for which the predicates are defined.</typeparam>
    /// <param name="leftExpr">
    /// The existing predicate to extend. Can be <c>null</c>, in which case the <paramref name="rightExpr"/> is returned.
    /// </param>
    /// <param name="rightExpr">
    /// The new predicate to combine with the existing one.
    /// </param>
    /// <param name="merge">
    /// A function that defines how to combine the two expressions (e.g., <see cref="Expression.OrElse(Expression, Expression)"/> or <see cref="Expression.AndAlso(Expression, Expression)"/>).
    /// </param>
    /// <returns>
    /// A combined predicate expression based on the specified merge function.
    /// </returns>
    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>>? leftExpr,
        Expression<Func<T, bool>> rightExpr,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        if (leftExpr == null)
            return rightExpr;

        ParameterExpression? parameter = Expression.Parameter(typeof(T), "x");

        Expression? left = new ReplaceExpressionVisitor(leftExpr.Parameters[0], parameter)
            .Visit(leftExpr.Body) ?? throw new InvalidOperationException("Left expression visitor returned null.");

        Expression? right = new ReplaceExpressionVisitor(rightExpr.Parameters[0], parameter)
            .Visit(rightExpr.Body) ?? throw new InvalidOperationException("Right expression visitor returned null.");

        BinaryExpression? combined = merge(left, right);
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
