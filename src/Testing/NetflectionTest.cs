using System.Linq.Expressions;
using JuliaDotNet;

namespace Testing;

public class Tests
{
    private NetReflection bc;
    private Tester tstr, tstrb;
    
    [SetUp]
    public void Setup() {
        bc = new NetReflection();
        tstr = new();
        tstrb = new() { a = 4 };
    }

    [Test]
    public void Test1()
    {
        bc.Set(tstr, "a", 123, false);
        Assert.That(123, Is.EqualTo(bc.Get(tstr, "a", false)));
    
        bc.Set(typeof(Tester), "A", 23424, false);
        Assert.That(23424, Is.EqualTo(bc.Get(typeof(Tester), "A", false)));
    
        bc.Set(typeof(Tester), "B", 23424, false);
        Assert.That(23424, Is.EqualTo(bc.Get(typeof(Tester), "B", false)));
    
        bc.Set(tstr, "b", 123, false);
        Assert.That(123, Is.EqualTo(bc.Get(tstr, "b", false)));

        Assert.That(36, Is.EqualTo(bc.Invoke(typeof(Tester), "Add", [34], false)));
        Assert.That(38L, Is.EqualTo(bc.Invoke(typeof(Tester), "Add", [34L], false)));
    
        Assert.That(23 + 3 + 43, Is.EqualTo(bc.Invoke(tstr, "Add2", [23, 43], false)));
        Assert.That(23L + 43L + 5, Is.EqualTo(bc.Invoke(tstr, "Add2", [23L, 43L], false)));

        bc.BinaryOperation(ExpressionType.Add, tstr, tstrb);
        bc.UnaryOperation(tstr, ExpressionType.OnesComplement);
    
        bc.SetIndex(tstr, 3, [4], false);
        bc.Invoke(tstr, "CallVoidMethod", Span<object>.Empty, true);
        Assert.That(51, Is.EqualTo(tstr.a));
        
        bc.Invoke(typeof(Tester), "CallStaticVoidMethod", Span<object>.Empty, true);
        Assert.That(71, Is.EqualTo(Tester.B));
        // Assert.That(4, Is.EqualTo(bc.GetIndex(tstr, [3])));
    }
    
}

public class Tester
{
    public static int A { get; set; } = 3;
    public int a { get; set; } = 2;

    public int b;
    public static int B;
    
    public static int Add(int x) => x + 2;
    public static long Add(long x) => x + 4;
    
    public int Add2(int a, int b) => a + 3 + b;
    public long Add2(long a, long b) => a + 5 + b;

    public void CallVoidMethod() { a = 51;}

    public static void CallStaticVoidMethod() {
        B = 71;
    }

    public int this[int j] {
        get => b;
        set => b = value;
    }
    
    public static Tester operator+(Tester a, Tester b) => new(){a = a.a + b.a};
    public static Tester operator~(Tester a) => new(){a = ~a.a};

    public override string ToString() => a.ToString();
}
