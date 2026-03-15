**JuliaDotNet** is a bidirectional bridge between .NET (C#) and the Julia programming language. It enables developers to
leverage Julia's mathematical power within C# applications and interact with .NET objects directly from Julia scripts.

## Key Features

* **Dynamic Julia Interop**: Use the `JAny` type to interact with Julia objects as if they were native C# dynamic
  objects.
* **Automatic Memory Management**: Seamlessly handles Julia's Garbage Collector (GC) by automatically rooting objects
  via `JAny` to prevent premature collection.
* **Bidirectional Object Mapping**:
    * **Boxing**: Pass .NET primitives, strings, and custom objects into Julia.
    * **Unboxing**: Convert Julia results back into native .NET types (int, float, string, etc.).
* **High-Performance Arrays**: Direct access to Julia arrays using `Span<T>` for zero-copy memory operations where
  possible.

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

The Dynamic Bridge
The core of this interaction is the JAny struct. In C#, it acts as a thin wrapper around a Julia pointer (jl_value_t*),
but when cast to dynamic, it utilizes the DLR (Dynamic Language Runtime) to map C# member access directly to Julia's
internal metadata and function dispatch.

Internally this object is just a IntPtr that is wrapped with a .NET object. The library will internally pin this
reference based on the lifetime of the JAny object.

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
    public long a;
    
    public long this[long index] {
        get => a;
        set => a = value;
    }

    public override string ToString() => $"Test(a={a}) from the .NET side!";
}

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
* [ ] **Julia Native Operators**: Extend the `SharpObject` in Julia to support native operators.
  * Goal: Allow `csharp_obj1 + csharp_obj2` to automatically map to C# operator overloads instead of requiring `invokemem`.

### 3. Documentation & Tooling
* [ ] **API Reference**: Generate full Docstrings for the `JuliadotNet` Julia module.
* [ ] **Advanced Examples**: Add tutorials for integration

### 4. Robustness & Testing
* [ ] **Unit Test Expansion**: Increase coverage for edge cases in .NET Reflection, especially regarding Generics and Ref/Out parameters.
* [ ] **Stress Testing**: Longevity tests for cross-runtime Garbage Collection under heavy object churn.

---

## 🤝 Contributing

Contributions are welcome! If you are interested in extending the DLR capabilities or improving the memory management layer etc., please feel free to open an issue or a Pull Request.