using TypeShape.Applications.GraphSearch;
using Xunit;

namespace TypeShape.Tests
{
    public partial class GraphSearchTests
    {
        [Fact]
        public void RootOnly()
        {
            var root = new FirstPoco {Id = 1};
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 1);
            Assert.Same(root, match);
        }

        [Fact]
        public void Single()
        {
            var first = new FirstPoco {Id = 2};
            var root = new FirstPoco {Id = 1, First = first};
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 2);
            Assert.Same(first, match);
        }


        [Fact]
        public void SingleList()
        {
            var first = new FirstPoco {Id = 2};
            var root = new FirstPoco {Id = 1, FirstList = [first]};
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 2);
            Assert.Same(first, match);
        }

        [Fact]
        public void SingleArray()
        {
            var first = new FirstPoco {Id = 2};
            var root = new FirstPoco {Id = 1, FirstArray = [first]};
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 2);
            Assert.Same(first, match);
        }

        [Fact]
        public void SingleDictionary()
        {
            var first = new FirstPoco {Id = 2};
            var root = new FirstPoco {Id = 1, FirstDictionary = {{first.Id, first}}};
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 2);
            Assert.Same(first, match);
        }

        [Fact]
        public void Multiple()
        {
            var first = new FirstPoco { Id = 2 };
            var second = new SecondPoco {Id = 3};
            var root = new FirstPoco { Id = 1, First = first, Second = second};
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 3);
            Assert.Same(second, match);
        }


        [Fact]
        public void MultipleList()
        {
            var first = new FirstPoco { Id = 2 };
            var second = new SecondPoco { Id = 3 };
            var root = new FirstPoco { Id = 1, FirstList = [first], SecondList = [second] };
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 3);
            Assert.Same(second, match);
        }

        [Fact]
        public void MultipleArray()
        {
            var first = new FirstPoco { Id = 2 };
            var second = new SecondPoco { Id = 3 };
            var root = new FirstPoco { Id = 1, FirstArray = [first], SecondArray = [second] };
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 3);
            Assert.Same(second, match);
        }

        [Fact]
        public void MultipleDictionary()
        {
            var first = new FirstPoco {Id = 2};
            var second = new SecondPoco {Id = 3};
            var root = new FirstPoco
                {Id = 1, FirstDictionary = {{first.Id, first}}, SecondDictionary = {{second.Id, second}}};
            var match = GraphSearch.Find<FirstPoco, BasePoco>(root, x => x.Id == 3);
            Assert.Same(second, match);
        }


        [GenerateShape]
        public abstract partial class BasePoco
        {
            public long Id { get; set; }
            public BasePoco First { get; set; } = default!;
            public BasePoco? Second { get; set; }

            public List<BasePoco> FirstList { get; set; } = default!;
            public List<BasePoco?> SecondList { get; set; } = [];

            public BasePoco[] FirstArray { get; set; } = default!;
            public BasePoco?[] SecondArray { get; set; } = [];


            public Dictionary<long, BasePoco> FirstDictionary { get; set; } = default!;
            public Dictionary<long, BasePoco?> SecondDictionary { get; set; } = [];
        }

        [GenerateShape]
        public sealed partial class FirstPoco : BasePoco;

        [GenerateShape]
        public sealed partial class SecondPoco : BasePoco;
    }
}