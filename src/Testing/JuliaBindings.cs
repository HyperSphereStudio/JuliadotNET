using System.Numerics;
using JuliaDotNet;

namespace Testing;

public class JuliaBindings
{
    [SetUp]
    public void Setup()
    {
        Julia.Init(new JuliaOptions());
    }

    [Test]
    public void Test1()
    {
        dynamic f = Julia.Eval("f(x) = x[0]");
        Assert.That(3, Is.EqualTo((int)f(new[] { 3, 4, 5 })));

        /*
        dynamic a = Julia.Eval("""
                        Test = T"Test"
                        a = Test()
                        a[3] = 20 
                        a       
                   """);
        
        Assert.That((object) a[3], Is.EqualTo(20));
        */
    }
}

public class Test
{
    public static int A { get; set; } = 3;
    public int a { get; set; } = 2;

    public int b;
    public static int B;

    public static int Add(int x) => x + 2;
    public static long Add(long x) => x + 4;

    public int Add2(int a, int b) => a + 3 + b;
    public long Add2(long a, long b) => a + 5 + b;

    public void CallVoidMethod()
    {
        a = 51;
    }

    public static I TestGenericMethod<I>(I v) where I : IFloatingPoint<I>
    {
        return v + I.One;
    }

    public static void CallStaticVoidMethod()
    {
        B = 71;
    }

    public long this[long j]
    {
        get => b;
        set => b = (int)value;
    }

    public static Test operator +(Test a, Test b) => new Test() { b = a.b + b.b };
    public override string ToString() => $"Test(a={a}, b={b})";
}