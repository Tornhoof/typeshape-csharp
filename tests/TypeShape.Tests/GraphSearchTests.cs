using TypeShape.Applications.GraphSearch;
using TypeShape.Applications.RandomGenerator;
using Xunit;

namespace TypeShape.Tests
{
    public partial class GraphSearchTests
    {
        [Fact]
        public void RootOnly()
        {
            var root = new FirstPoco {Id = 1};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 1);
            Assert.Same(root, match);
        }

        [Fact]
        public void Single()
        {
            var first = new FirstPoco {Id = 2};
            var root = new FirstPoco {Id = 1, First = first};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 2);
            Assert.Same(first, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Same(first, imatch);
        }

        [Fact]
        public void SingleStruct()
        {
            var first = new StructData(2);
            var root = new FirstPoco {Id = 1, FirstStruct = first};

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Equal(first, imatch);
        }


        [Fact]
        public void SingleNullableStruct()
        {
            var first = new StructData(2);
            var root = new FirstPoco {Id = 1, SecondStruct = first};

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Equal(first, imatch);
        }

        [Fact]
        public void SingleList()
        {
            var first = new FirstPoco {Id = 2};
            var root = new FirstPoco {Id = 1, FirstList = [first]};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 2);
            Assert.Same(first, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Same(first, imatch);
        }

        [Fact]
        public void SingleStructList()
        {
            var first = new StructData(2);
            var root = new FirstPoco {Id = 1, FirstStructList = [first]};

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Equal(first, imatch);
        }


        [Fact]
        public void SingleNullableStructList()
        {
            var first = new StructData(2);
            var root = new FirstPoco {Id = 1, SecondStructList = [first]};

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Equal(first, imatch);
        }

        [Fact]
        public void SingleArray()
        {
            var first = new FirstPoco {Id = 2};
            var root = new FirstPoco {Id = 1, FirstArray = [first]};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 2);
            Assert.Same(first, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Same(first, imatch);
        }


        [Fact]
        public void SingleStructDictionary()
        {
            var first = new StructData(2);
            var root = new FirstPoco {Id = 1, FirstStructDictionary = new() {{first.Id, first}}};

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Equal(first, imatch);
        }


        [Fact]
        public void SingleNullableStructDictionary()
        {
            var first = new StructData(2);
            var root = new FirstPoco {Id = 1, SecondStructDictionary = new() {{first.Id, first}}};

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Equal(first, imatch);
        }


        [Fact]
        public void SingleDictionary()
        {
            var first = new FirstPoco {Id = 2};
            var root = new FirstPoco {Id = 1, FirstDictionary = new() {{first.Id, first}}};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 2);
            Assert.Same(first, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 2);
            Assert.Same(first, imatch);
        }

        [Fact]
        public void Multiple()
        {
            var first = new FirstPoco {Id = 2};
            var second = new SecondPoco {Id = 3};
            var root = new FirstPoco {Id = 1, First = first, Second = second};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 3);
            Assert.Same(second, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 3);
            Assert.Same(second, imatch);

            Assert.Null(GraphSearch.FindFirst<FirstPoco, FirstPoco>(root, x => x.Id == 3));
        }


        [Fact]
        public void MultipleList()
        {
            var first = new FirstPoco {Id = 2};
            var second = new SecondPoco {Id = 3};
            var root = new FirstPoco {Id = 1, FirstList = [first], SecondList = [second]};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 3);
            Assert.Same(second, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 3);
            Assert.Same(second, imatch);

            Assert.Null(GraphSearch.FindFirst<FirstPoco, FirstPoco>(root, x => x.Id == 3));
        }

        [Fact]
        public void MultipleArray()
        {
            var first = new FirstPoco {Id = 2};
            var second = new SecondPoco {Id = 3};
            var root = new FirstPoco {Id = 1, FirstArray = [first], SecondArray = [second]};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 3);
            Assert.Same(second, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 3);
            Assert.Same(second, imatch);

            Assert.Null(GraphSearch.FindFirst<FirstPoco, FirstPoco>(root, x => x.Id == 3));
        }

        [Fact]
        public void MultipleDictionary()
        {
            var first = new FirstPoco {Id = 2};
            var second = new SecondPoco {Id = 3};
            var root = new FirstPoco
                {Id = 1, FirstDictionary = new() {{first.Id, first}}, SecondDictionary = new() {{second.Id, second}}};
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 3);
            Assert.Same(second, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 3);
            Assert.Same(second, imatch);

            Assert.Null(GraphSearch.FindFirst<FirstPoco, FirstPoco>(root, x => x.Id == 3));
        }


        [Fact]
        public void SimpleNested()
        {
            var third = new FirstPoco() { Id = 4};
            var second = new SecondPoco { Id = 3, First = third};
            var root = new FirstPoco { Id = 2, Second = second };
            var match = GraphSearch.FindFirst<FirstPoco, BasePoco>(root, x => x.Id == 4);
            Assert.Same(third, match);

            var imatch = GraphSearch.FindFirst<FirstPoco, IMyInterface>(root, x => x.Id == 4);
            Assert.Same(third, imatch);
        }

        [GenerateShape]
        public abstract partial class BasePoco : IMyInterface
        {
            public long Id { get; set; }
            public FirstPoco First { get; set; } = default!;
            public SecondPoco? Second { get; set; }

            public List<FirstPoco> FirstList { get; set; } = default!;
            public List<SecondPoco> SecondList { get; set; } = [];

            public FirstPoco[] FirstArray { get; set; } = default!;
            public SecondPoco[] SecondArray { get; set; } = [];

            public Dictionary<long, FirstPoco> FirstDictionary { get; set; } = default!;
            public Dictionary<long, SecondPoco> SecondDictionary { get; set; } = [];

            public StructData FirstStruct { get; set; } = default!;
            public StructData? SecondStruct { get; set; }

            public List<StructData> FirstStructList { get; set; } = default!;
            public List<StructData?> SecondStructList { get; set; } = [];

            public StructData[] FirstStructArray { get; set; } = default!;
            public StructData?[] SecondStructArray { get; set; } = [];

            public Dictionary<long, StructData> FirstStructDictionary { get; set; } = default!;
            public Dictionary<long, StructData?> SecondStructDictionary { get; set; } = [];
        }

        [GenerateShape]
        public sealed partial class FirstPoco : BasePoco;

        [GenerateShape]
        public sealed partial class SecondPoco : BasePoco;

        public interface IMyInterface
        {
            long Id { get; }
        }

        public readonly struct StructData : IMyInterface, IEquatable<StructData>
        {
            public StructData(long id)
            {
                Id = id;
            }

            public long Id { get; }

            public bool Equals(StructData other)
            {
                return Id == other.Id;
            }

            public override bool Equals(object? obj)
            {
                return obj is StructData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }
    }
}