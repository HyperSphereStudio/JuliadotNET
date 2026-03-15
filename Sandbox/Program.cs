using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using JuliaDotNet;
using static JuliaDotNet.JuliaCalls;

try
{
    Julia.Init(new JuliaOptions());
    
    Julia.Eval("""
                   # Create a resolver to find .NET types
                   nr = NetNameResolver()
                   
                   # Import the C# "Test" class into Julia
                   Test = usingname(nr, "Test")
                   
                   # Instantiate the C# class from INSIDE Julia
                   a = Test()
                   
                   println(a)          # Calls the C# .ToString() override
                   
                   # Use Julia indexing syntax to call the C# indexer
                   a[3] = 20           
                   
                   println(a)          # Confirm the value changed
                   
                   res1 = invokemem(Test, :Add, Int32(10)) # Calls int overload -> 12
                   res2 = invokemem(Test, :Add, Int64(10)) # Calls long overload -> 14
                   
                   res3 = invokemem(a, :Add2, 7, 9)     
                   
                   invokememvoid(a, :CallVoidMethod)       #Use this to invoke void functions     
                   println(a)
               """);
    
    Julia.Exit(0);
}
catch (Exception e)
{
    Console.WriteLine(e);
}

public class Test {
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

    public long this[long j] {
        get => b;
        set => b = (int) value;
    }

    public override string ToString() => $"Test(a={a}, b={b})";
}