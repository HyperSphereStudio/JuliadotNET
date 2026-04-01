**JuliaDotNet** is a bidirectional bridge between .NET (C#) and the Julia programming language. It enables developers to
leverage Julia's mathematical power within C# applications and interact with .NET objects directly from Julia scripts.

## Key Features

* **Dynamic Julia Interop**: Use the `JAny` type to interact with Julia objects as if they were native C# dynamic
  objects.
* **Automatic Memory Management**: Handles Julia's Garbage Collector (GC) by automatically rooting objects
  via `JAny` to prevent premature collection.
* **Bidirectional Object Mapping**:
    * **Boxing**: Pass .NET primitives, strings, and custom objects into Julia.
    * **Unboxing**: Convert Julia results back into native .NET types (int, float, string, etc.).

##  System Requirements

* **.NET 8.0+**
* **Julia 1.11+**


## Initialization

```cs

using JuliaDotNet;

// Optional configuration
var options = new JuliaOptions {
    ThreadCount = 4,
    Optimize = 3
};

// Initializes the Julia engine
Julia.Init(options);

```

## Julia To Sharp Interop

The JuliaDotNet library bridges the gap between the static world of .NET and the highly dynamic nature of Julia. By
implementing the DynamicObject pattern in C#, the library essentially exposes the Julia runtime as a first-class dynamic
environment within the .NET ecosystem.

The core of this interaction is the JAny struct. In C#, it acts as a thin wrapper around a Julia pointer (jl_value_t*),
but when cast to dynamic, it utilizes the DLR (Dynamic Language Runtime) to map C# member access directly to Julia's
internal metadata and function dispatch.

Internally this object is just a IntPtr that is wrapped with a .NET object. The library will internally pin this
reference based on the lifetime of the JAny object.

### Type Resolution & Instantiation

```julia
# Resolve .NET types using the T macro
List = T"System.Collections.Generic.List`1"
Int32 = T"System.Int32"

# Instantiate generic collections
my_list = List[Int32]()
invokememvoid(my_list, :Add, 5)
```

### Delegate Bridging
```julia
# Create a .NET Predicate<int> from a Julia lambda
pred = delegate(T"System.Predicate`1"[Int32], x -> x == 5)

# Use the C# method with the Julia delegate
invokemem(my_list, :RemoveAll, pred)
```

### Operator Mapping
```julia
# Maps directly to C# operator overloads
println(a + b)
println(a < b)
```

### Example

```csharp

dynamic Rocket = Julia.Eval(@"
    mutable struct Rocket
        name::String
        fuel::Float64
        is_active::Bool
        Rocket() = new()
    end

    function launch!(r::Rocket)
        r.fuel -= 10.5
        println(""Rocket $(r.name) launched! Fuel remaining: $(r.fuel)"")
        return r.fuel
    end
    
    Rocket
");

    dynamic launchf = Julia.Eval("launch!");
    dynamic rocket = Rocket(); 
    
    rocket.name = "My Rocket!";
    rocket.fuel = 32;
    launchf(rocket);      
    Console.WriteLine(rocket);      //Rocket("My Rocket!", 21.5, false)

```

The library provides two ways to interact with the Julia runtime: a low-level P/Invoke layer and a high-level dynamic
wrapper.

Direct libjulia Calls
The JuliaCalls class exposes the raw C API of Julia. While this gives you absolute control, it requires manual memory
management and constant handling of IntPtr (representing the underlying jl_value_t*).

```csharp

using static JuliaDotNet.JuliaCalls;

// Raw P/Invoke: Fast but dangerous
IntPtr rawResult = jl_eval_string("1 + 1");
// You must ensure this pointer is rooted if you intend to keep it!
```

### Arrays

When working with performance-critical operations like FFTs, Linear Algebra, or Signal Processing, you can use
JuliaArrays.WrapPtrToArray to let Julia operate directly on C# fixed memory buffers.

```csharp

using System.Numerics;
using JuliaDotNet;

public class FastFourierExample
{
    // We store the Julia function as a dynamic object
    public static dynamic DoFFT { get; private set; }
    
    public static unsafe void ComputeFFT(Complex[] x, Complex[] output, bool isInverse) {
        if(x.Length != output.Length)
            throw new Exception("Invalid Data Length!");
            
        // 1. Pin the C# memory so the GC doesn't move it
        fixed (Complex* v = x) {
            fixed (Complex* w = output) {
                // 2. Wrap the pointers into Julia Arrays (Zero-Copy)
                using var ja = new JAny(JuliaArrays.WrapPtrToArray(v, x.Length));
                using var ob = new JAny(JuliaArrays.WrapPtrToArray(w, x.Length));
                
                // 3. Call the Julia function directly
                DoFFT(isInverse, ja, ob);
            }
        }
    }    

    public static void Init() {
        // Define the heavy-lifting logic in Julia
        DoFFT = Julia.Eval(
            """
            using FFTW
            
            function do_fft(isInverse, x, o)
                # Use Julia's FFTW plans to operate on the C# memory buffers
                flags = FFTW.ESTIMATE | FFTW.UNALIGNED
                p = isInverse ? plan_ifft(x; flags=flags) : plan_fft(x; flags=flags)
                FFTW.mul!(o, p, x)
            end 
            """);
    }

    public static void Run() {
        Julia.Init(new JuliaOptions());
        Init();

        Complex[] values = new Complex[1024];
        Complex[] outputs = new Complex[1024];
        
        // Initialize data...
        for (int i = 0; i < values.Length; i++) 
            values[i] = new Complex(i, 0);

        // Compute FFT using Julia's FFTW
        ComputeFFT(values, outputs, false);
        
        Console.WriteLine("FFT computed successfully using shared memory!");
    }
}
```

## Sharp to Julia Interop

The library exposes the .NET DLR to Julia. you can pass native C# objects into the Julia runtime. Julia can then treat
these objects as first-class entities: accessing fields, calling methods.

```csharp

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
	
	
    public override string ToString() => $"Test(a={a}, b={b})";
	
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

Julia.Eval("""
	# Import the C# "Test" class into Julia
    Test = T"Test"
                   
    # Instantiate the C# class from INSIDE Julia
    a = Test()
                   
    println(a)          # Calls the C# .ToString() override
                   
    # Use Julia indexing syntax to call the C# indexer
    a[3] = 20           
                   
    println(a)          					# Confirm the value changed
                   
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
                   
    T"System.Collections.Generic"        	#Add to namespace
    T"System"
                   
	list = T"List`1"
    int = T"Int64"
                   
    myList = list[int]()					#Create generic type with type[type...]
    invokememvoid(myList, :Add, 5)
                   
    myArrayType = int[1];
                   
    usingasm(; asm_name="System.Console")
    console = T"Console"
    invokememvoid(console, :WriteLine, "Hello From Julia Console")
    pred = delegate(T"System.Predicate`1"[int], x -> x == 5)      #Wrap function to delegate
                   
    println(collect(myList));          # [5, 6]
    invokemem(myList, :RemoveAll, pred)
    println(collect(myList));          # [6]   """)
```

## Shutdown

To Release All Objects Held by Julia and Run Finalizers

```cs
Julia.Exit(0);
```


## 🛠 Upcoming Roadmap (TODO)

### 1. Reverse Initialization (Julia ➔ .NET)
* [ ] **Standalone Julia Entry**: Currently, the bridge is primary-managed by C#. Working on a mechanism to launch the .NET runtime directly from a Julia session (`using JuliaDotNet`) without an existing C# host process.

### 2. Deep DLR Integration
* [ ] **Julia Native Operators**: Add the rest of the .NET operators (only a small subset are implemented atm)
### 3. Documentation & Tooling
* [ ] **Advanced Examples**: Add tutorials for integration

### 4. Robustness & Testing
* [ ] **Unit Test Expansion**: Increase coverage for edge cases in .NET Reflection, especially regarding Generics and Ref/Out parameters.
* [ ] **Stress Testing**: Longevity tests for cross-runtime Garbage Collection under heavy object churn.

---

## 🤝 Contributing

Contributions are welcome! If you are interested in extending the DLR capabilities or improving the memory management layer etc., please feel free to open an issue or a Pull Request.