namespace TypeShape.Applications.GraphSearch;

using TypeShape;
using Abstractions;


public static partial class GraphSearch
{
    public delegate TMatch? MatchDelegate<in T, out TMatch>(T value, Predicate<TMatch> predicate);

    public static MatchDelegate<T, TMatch> Create<T, TMatch>(ITypeShape<T> type) where T : TMatch
        => new Builder<TMatch>().BuildDelegate(type);

    public static MatchDelegate<T, TMatch> Create<T, TMatch>(ITypeShapeProvider provider) where T : TMatch
        => Create<T, TMatch>(provider.Resolve<T>());

    public static TMatch? FindFirst<T, TMatch>(this MatchDelegate<T, TMatch> matcher, T value, Predicate<TMatch> predicate)
        where T : TMatch
    {
        ArgumentNullException.ThrowIfNull(value);
        if (predicate(value))
        {
            return value;
        }

        return matcher(value, predicate);
    }

    public static TMatch? FindFirst<T, TMatch>(T value, Predicate<TMatch> predicate) where T : ITypeShapeProvider<T>, TMatch
        => GraphSearchCache<T, TMatch, T>.Value(value, predicate);

    public static TMatch? FindFirst<T, TMatch, TProvider>(T value, Predicate<TMatch> predicate) where T : TMatch
        where TProvider : ITypeShapeProvider<T>
        => GraphSearchCache<T, TMatch, TProvider>.Value(value, predicate);


    private static class GraphSearchCache<T, TMatch, TProvider> where T : TMatch where TProvider : ITypeShapeProvider<T>
    {
        public static MatchDelegate<T, TMatch> Value => s_Value ??= Create<T, TMatch>(TProvider.GetShape());
        private static MatchDelegate<T, TMatch>? s_Value;
    }
}