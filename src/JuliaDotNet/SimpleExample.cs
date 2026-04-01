using System.Numerics;

namespace JuliaDotNet;

public class SimpleExample
{
    public static dynamic DoFFT { get; private set; }
    
    public static unsafe void ComputeFFT(Complex[] x, Complex[] output, bool isInverse) {
        if(x.Length != output.Length)
            throw new Exception("Invalid Data Length!");
            
        fixed (Complex* v = x) {
            fixed (Complex* w = output) {
                var ja = new JAny(JuliaArrays.WrapPtrToArray(v, x.Length));
                var ob = new JAny(JuliaArrays.WrapPtrToArray(w, x.Length));
                DoFFT(isInverse, ja, ob);
            }
        }
    }    

    public static void Init() {
        DoFFT = Julia.Eval(
            $"""
                  using FFTW
                  
                  function do_fft(isInverse, x, o)
                        flags = FFTW.ESTIMATE | FFTW.UNALIGNED
                        FFTW.mul!(o, isInverse ? plan_ifft(x; flags=flags) : plan_fft(x; flags=flags), x)
                  end 
                
            """);
        
    }

    public static void SimpleExamples() {
        Julia.Init(new JuliaOptions());
        
        Complex[] values = new Complex[10];
        Complex[] outputs = new Complex[10];
        Complex[] values2 = new Complex[10];
        for (var i = 0; i < 10; i++)
            values[i] = new Complex(30 - i, 30 + i);
    
        SimpleExample.Init();
    
        Console.WriteLine("FFT:");
        Console.WriteLine("Inputs:" + string.Join(", ", values));
        SimpleExample.ComputeFFT(values, outputs, false);
        Console.WriteLine("Outputs:" + string.Join(", ", outputs));
    
        Console.WriteLine("IFFT:");
        SimpleExample.ComputeFFT(outputs, values2, true);
        Console.WriteLine("Inputs:" + string.Join(", ", values2));
    }
}