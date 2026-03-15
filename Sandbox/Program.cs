using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using JuliaDotNet;
using static JuliaDotNet.JuliaCalls;

try
{
    Julia.Init(new JuliaOptions());
    
    Julia.Eval("""
                nr = NetNameResolver()
                Test = usingname(nr, "Test")
                
                a = Test()
                println(a)
                a[3] = 20
                println(a)
                            
                """);
    
    Julia.Exit(0);
}
catch (Exception e)
{
    Console.WriteLine(e);
}

public class Test {
    public long a;
    
    public long this[long index] {
        get => a;
        set => a = value;
    }

    public override string ToString() => $"Test(a={a}).NET";
}