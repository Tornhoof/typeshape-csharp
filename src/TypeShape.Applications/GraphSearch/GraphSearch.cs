namespace TypeShape.Applications.GraphSearch;

using TypeShape;
using Abstractions;

public delegate TMatch? BreadthFirstSearch<in T, out TMatch>(T? value, Predicate<TMatch> predicate);
public delegate TMatch? DepthFirstSearch<in T, out TMatch>(T? value, Predicate<TMatch> predicate);

public static partial class GraphSearch
{
    public static BreadthFirstSearch<T, TMatch> CreateBfs<T, TMatch>(ITypeShape<T> type)
        => new Builder().BuildBreadthFirstSearch<T, TMatch>(type);

    public static DepthFirstSearch<T, TMatch> CreatDfs<T, TMatch>(ITypeShape<T> type)
        => new Builder().BuildDepthFirstSearch<T, TMatch>(type);

    public static BreadthFirstSearch<T, TMatch> CreateBfs<T, TMatch>(ITypeShapeProvider provider)
        => CreateBfs<T, TMatch>(provider.Resolve<T>());

    public static DepthFirstSearch<T, TMatch> CreatDfs<T, TMatch>(ITypeShapeProvider provider)
        => CreatDfs<T, TMatch>(provider.Resolve<T>());

    public static TMatch? Dfs<T, TMatch>(this DepthFirstSearch<T, TMatch> dfs, T? value, Predicate<TMatch> predicate)
    {
        if (value is null)
        {
            return default;
        }
        return dfs(value, predicate);
    }

    public static TMatch? Bfs<T, TMatch>(this BreadthFirstSearch<T, TMatch> bfs, T? value, Predicate<TMatch> predicate)
    {
        if (value is null)
        {
            return default;
        }
        return bfs(value, predicate);
    }

    public static TMatch? Dfs<T, TMatch>(T? value, Predicate<TMatch> predicate) where T : ITypeShapeProvider<T>
        => GraphSearchCache<T, TMatch, T>.Dfs(value, predicate);

    public static TMatch? Dfs<T, TMatch, TProvider>(T? value, Predicate<TMatch> predicate)
        where TProvider : ITypeShapeProvider<T>
        => GraphSearchCache<T, TMatch, TProvider>.Dfs(value, predicate);

    public static TMatch? Bfs<T, TMatch>(T? value, Predicate<TMatch> predicate) where T : ITypeShapeProvider<T>
        => GraphSearchCache<T, TMatch, T>.Bfs(value, predicate);

    public static TMatch? Bfs<T, TMatch, TProvider>(T? value, Predicate<TMatch> predicate)
        where TProvider : ITypeShapeProvider<T>
        => GraphSearchCache<T, TMatch, TProvider>.Bfs(value, predicate);

    private static class GraphSearchCache<T, TMatch, TProvider> where TProvider : ITypeShapeProvider<T>
    {
        public static BreadthFirstSearch<T, TMatch> Bfs => s_bfs ??= CreateBfs<T, TMatch>(TProvider.GetShape());
        private static BreadthFirstSearch<T, TMatch>? s_bfs;
        public static DepthFirstSearch<T, TMatch> Dfs => s_dfs ??= CreatDfs<T, TMatch>(TProvider.GetShape());
        private static DepthFirstSearch<T, TMatch>? s_dfs;
    }
}