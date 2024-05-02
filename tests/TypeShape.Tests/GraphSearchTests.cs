using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeShape.Applications.GraphSearch;
using Xunit;

namespace TypeShape.Tests
{
    public class GraphSearchTests
    {
        [Fact]
        public void SingleNode()
        {
            var node = new Node {Id = 1};
            var bfs = GraphSearch.Bfs<Node, Node>(node, x => x.Id == 1);
            var dfs = GraphSearch.Bfs<Node, Node>(node, x => x.Id == 1);
            Assert.Same(node, bfs);
            Assert.Same(node, dfs);
        }
    }

    [GenerateShape]
    public sealed partial class Node
    {
        public long Id { get; set; }
        public Node? Sibling { get; set; }
        public List<Node> Children { get; set; } = new();
    }
}