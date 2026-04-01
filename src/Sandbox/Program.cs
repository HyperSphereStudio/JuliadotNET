using System.Numerics;
using JuliaDotNet;

try {

    Julia.Init(new JuliaOptions());

    Julia.Eval("""
                   # Import the C# "Test" class into Julia
                   Test = T"Test"
                   
                   # Instantiate the C# class from INSIDE Julia
                   a = Test()
                   b = Test()
               
                   b.b = Int32(30);
                   
                   println(a)          # Calls the C# .ToString() override
                   
                   # Use Julia indexing syntax to call the C# indexer
                   a[3] = 20           
                   
                   println(a)          # Confirm the value changed
                   
                   res1 = invokemem(Test, :Add, Int32(10)) # Calls int overload -> 12
                   res2 = invokemem(Test, :Add, Int64(10)) # Calls long overload -> 14
                   
                   println(invokemem(Test, :TestGenericMethod, 43.2))  #Call generic method -> 44.2   
                   
                   res3 = invokemem(a, :Add2, 7, 9) 
                   
                   println("a+b=", a + b)               #Overload the c# operator  
                   println("a-b=", a - b)            
                   println("a<b=", a < b)           
                   println("a==b=", a == b)              
                   println(-a)                  
                   
                   invokememvoid(a, :CallVoidMethod)       #Use this to invoke void functions     
                   println(a)
                   
                   T"System.Collections.Generic"        #Add to namespace
                   T"System"
                   
                   list = T"List`1"
                   int = T"Int64"
                   
                   myList = list[int]()
                   invokememvoid(myList, :Add, 5)
                   invokememvoid(myList, :Add, 6)
                   
                   myArrayType = int[1];
                   
                   usingasm(; asm_name="System.Console")
                   console = T"Console"
                   invokememvoid(console, :WriteLine, "Hello From Julia Console")
               
                   pred = delegate(T"System.Predicate`1"[int], x -> x == 5)      #Wrap function to delegate
                   
                   println(collect(myList));          # [5, 6]
                   invokemem(myList, :RemoveAll, pred)
                   println(collect(myList));          # [6]
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

    public static I TestGenericMethod<I>(I v) where I: IFloatingPoint<I> {
        return v + I.One;
    }
    
    public static void CallStaticVoidMethod() {
        B = 71;
    }

    public long this[long j] {
        get => b;
        set => b = (int) value;
    }

    public static Test operator +(Test a) => new Test() { b = +a.b };
    public static Test operator -(Test a) => new Test() { b = -a.b };
    
    public static bool operator <(Test a, Test b) => a.b < b.b;
    public static bool operator >(Test a, Test b) =>  a.b > b.b;
    public static bool operator <=(Test a, Test b) => a.b <= b.b;
    public static bool operator >=(Test a, Test b) =>  a.b >= b.b;
    public static Test operator +(Test a, Test b) => new Test() { b = a.b + b.b };
    public static Test operator -(Test a, Test b) => new Test() { b = a.b - b.b };
    public static bool operator ==(Test a, Test b) => true;
    public static bool operator !=(Test a, Test b) => a.b != b.b;
    public override string ToString() => $"Test(a={a}, b={b})";
}