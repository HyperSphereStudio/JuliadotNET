using Documenter
using JuliaDotNet

makedocs(
    sitename = "JuliaDotNet.jl",
    modules = [JuliaDotNet],
    format = Documenter.HTML(),
    pages = [
        "Home" => "index.md",
        "API Reference" => "api.md"
    ]
)