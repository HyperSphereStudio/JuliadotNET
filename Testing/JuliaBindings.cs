using JuliaDotNet;

namespace Testing;

public class JuliaBindings {
    [SetUp]
    public void Setup() {
        Julia.Init(new JuliaOptions());
    }

    [Test]
    public void Test1() {
        dynamic f = Julia.Eval("f(x) = x[0]");
        Assert.That(3, Is.EqualTo((int) f(new[] { 3, 4, 5 })));
        
        
    }
}