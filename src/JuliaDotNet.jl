module JuliaDotNet
    export invokemem, invokememvoid, JulianArg, SharpObject, usingasm, usingname, NetNameResolver, nettypeof, @T_str, load_net, binaryop, unaryop, rawdelegate, delegate, @rawdelegate
    using Libdl

    const __roots_lock = ReentrantLock()
    const __object_roots = Dict{Int64, Any}()
    const __next_id = Threads.Atomic{Int64}(1)
    const _sharpfinalizer = Ref{Ptr{Cvoid}}()
    const _netreflect = Ref{Ptr{Cvoid}}()
    const _nettostring = Ref{Ptr{Cvoid}}()
    const _nettohash = Ref{Ptr{Cvoid}}()
    const _nettotype = Ref{Ptr{Cvoid}}()
    
    """
        SharpObject(handle::Int)
        A proxy object representing a live instance or Type in the .NET Runtime.
        The `handle` is a unique integer mapping to the object in the .NET side.
        This object automatically notifies .NET to release the reference when GC'd in Julia.
    """
    mutable struct SharpObject 
        const handle::Int
        
        function SharpObject(handle::Int)
            obj = new(handle)
            finalizer(obj) do x
                ccall(_sharpfinalizer[], Cvoid, (Int, ), getfield(x, :handle))
            end
            obj
        end
    end

"""
    ArgFlags::Enum

    Flags used to control marshaling behavior between Julia and .NET.
    - `Nothing`: Default behavior.
    - `TryConvertToSharpNative`: Force conversion to .NET primitives (int, double, etc).
    - `TryConvertToJuliaNative`: Force conversion of .NET results to Julia primitives.
    - `DiscardReturn`: Optimizes call by ignoring the return value (void).
"""
    @enum ArgFlags::Int begin
        Nothing = 0
        TryConvertToSharpNative = 1
        TryConvertToJuliaNative = 2
        DiscardReturn = 4
    end

"""
    JulianArg(x, flags::ArgFlags)

    Wraps a Julia value with specific instructions on how .NET should treat it
    during a method invocation.
"""
    struct JulianArg
        x::Any                      #IntPtr
        flags::ArgFlags
        
        JulianArg(x::Any, flags::ArgFlags = TryConvertToSharpNative) = new(x, flags)
    end

"""
    nettypeof(x::SharpObject) -> SharpObject

Returns the `System.Type` of the .NET object.
"""
    nettypeof(x::SharpObject) = ccall(_nettotype[], Any, (Int, ), handle(x))
    Base.hash(x::SharpObject) = ccall(_nettohash[], Int, (Int, ), handle(x))
    Base.string(x::SharpObject) = ccall(_nettostring[], Any, (Int, ), handle(x))
    Base.print(io::IO, x::SharpObject) = print(io, string(x))
    Base.show(io::IO, x::SharpObject) = show(io, string(x))
    Base.iterate(x::SharpObject) = iterate(x, invokemem(x, :GetEnumerator))
    Base.length(x::SharpObject) = x.Count
    
	function Base.iterate(x::SharpObject, en)
        if invokemem(en, :MoveNext)
            return (en.Current, en)
        else 
            return nothing
        end
    end
	
	"""
        rawdelegate(dtype::SharpObject, f::Ptr{Cvoid}) -> SharpObject
    
    Converts a raw C function pointer into a .NET Delegate of type `dtype`.
    """
	rawdelegate(dtype::SharpObject, f::Ptr{Cvoid}) = invokemem(MarshalTy, :GetDelegateForFunctionPointer, f, dtype)
	
	"""
        delegate(dtype::SharpObject, f::Function; returnflags, paramflags) -> SharpObject
    
    Wraps a Julia function `f` into a .NET Delegate.
    - `dtype`: The .NET Delegate Type (e.g., `T"System.Action"`).
    - `paramflags`: A vector of `ArgFlags` corresponding to function arguments.
    """
	function delegate(dtype::SharpObject, f; returnflags=TryConvertToSharpNative, paramflags=nothing)
        pflags = paramflags !== nothing ? Int(pointer(paramflags)) : 0
        nParams = paramflags !== nothing ? length(paramflags) : 0
        invokemem(SharpInteropTy, :CreateDelegateFromJuliaObject, dtype, f, pflags, nParams, Int(returnflags))
	end
	
	"""
        usingasm(; asm_name=nothing, file_path=nothing)
    
    Loads a .NET assembly into the current process.
    Use `asm_name` for GAC/Standard libs (e.g., "System.Text.Json").
    Use `file_path` for local DLLs.
    """
    function usingasm(x::SharpObject; asm_name=nothing, file_path=nothing)
		if asm_name !== nothing
			invokemem(x, :UsingAssemblyByName, asm_name)
		elseif file_path !== nothing
			invokemem(x, :UsingAssemblyByPath, file_path)
		else 
			error("Assembly Name or File Path must not be null")
		end
	end

"""
    usingname(name::String) -> SharpObject

Resolves a .NET type name and returns its `SharpObject` representation.
Example: `usingname("System.Math")`.
"""
    usingname(x::SharpObject, name::String) = invokemem(x, :Using, name)
    usingname(name::String) = usingname(DefaultResolver, name)
    usingasm(; asm_name=nothing, file_path=nothing) = usingasm(DefaultResolver; asm_name=asm_name, file_path=file_path)
    
    """
        T"TypeName"
    
    Convenience string macro for `usingname`. 
    Example: `T"System.Console".WriteLine("Hello")`.
    """
    macro T_str(name::String)
        usingname(name)
    end
    
    Base.getproperty(x::SharpObject, s::Symbol; returnflags = TryConvertToJuliaNative) = net_reflect(x, :getproperty, s; returnflags = returnflags)
    Base.getproperty(x::JulianArg, s::Symbol) = getproperty(x.x, s; returnflags=x.flags)
    
	Base.setproperty!(x::SharpObject, s::Symbol, v; returnflags = TryConvertToJuliaNative) = net_reflect(x, :setproperty, s, v; returnflags = returnflags)
	Base.setproperty!(x::JulianArg, s::Symbol, v) = setproperty!(x.x, s, v; returnflags=x.flags)
	
	Base.getindex(x::SharpObject, idxs...; returnflags = TryConvertToJuliaNative) = net_reflect(x, :getindex, idxs...; returnflags = returnflags)
    Base.getindex(x::JulianArg, idxs...) = getindex(x.x, idxs...; returnflags=x.flags)
   
    Base.setindex!(x::SharpObject, v, idxs...; returnflags = TryConvertToJuliaNative) = net_reflect(x, :setindex!, v, idxs...; returnflags = returnflags)
    Base.setindex!(x::JulianArg, v, idxs...) = setindex!(x.x, v, idxs...; returnflags=x.flags)
 
 """
     binaryop(op::Symbol, a, b)
 
 Invokes a .NET operator (e.g., :Add, :Subtract) between two objects.
 """
	binaryop(op::Symbol, a, b; returnflags = TryConvertToJuliaNative) = net_reflect(NetNameResolver, :binary_op, op, a, b; returnflags = returnflags)
	
	"""
        unaryop(op::Symbol, a)
    
    Invokes a .NET unary operator (e.g., :Negate).
    """
	unaryop(op::Symbol, a; returnflags=TryConvertToJuliaNative) = net_reflect(NetNameResolver, :unary_op, op, a; returnflags = returnflags)
	
	for (op, sop) in [(:+, :UnaryPlus), (:-, :Negate)]
		n = QuoteNode(sop)
		o = op
		eval(quote 
			Base.$o(a::Union{SharpObject, JulianArg}) = unaryop($n, a)
		end)
	end
	
	for (op, sop) in [(:+, :Add), (:-, :Subtract), (:*, :Multiply), (:/, :Divide),
		(:<, :LessThan), (:<=, :LessThanOrEqualTo), 
		(:>, :GreaterThan), (:>=, :GreaterThanOrEqualTo), 
		(:(==), :Equal), (:(!=), :NotEqual)]
		n = QuoteNode(sop)
		o = op
		eval(quote 
			Base.$o(a::Union{SharpObject, JulianArg}, b) = binaryop($n, a, b)
			Base.$o(a, b::Union{SharpObject, JulianArg}) = binaryop($n, a, b)
			Base.$o(a::Union{SharpObject, JulianArg}, b::Union{SharpObject, JulianArg}) = binaryop($n, a, b)
		end)
	end
	
    (x::SharpObject)(args...; returnflags=TryConvertToJuliaNative) = net_reflect(x, :invoke, args...; returnflags = returnflags)
    (x::JulianArg)(args...) = x.x(args...; returnflags=x.flags)
    
    """
        invokemem(obj, method::Symbol, args...)
    
    Dynamic method invocation on a .NET object.
    """
    invokemem(x::SharpObject, name::Symbol, args...; returnflags = TryConvertToJuliaNative) = net_reflect(x, :invokeMember, name, args...; returnflags = returnflags)
    invokemem(x::JulianArg, name::Symbol, args...; returnflags = TryConvertToJuliaNative) = invokemem(x.x, name, args...; returnflags=x.flags)
   
   """
       invokememvoid(obj, method::Symbol, args...)
   
   Calls a .NET method but discards the return value.
   Required for any function that returns void!
   """
    invokememvoid(x::SharpObject, name::Symbol, args...) = invokemem(x, name, args...; returnflags=DiscardReturn)

    handle(so::SharpObject) = getfield(so, :handle)
    create_sharp_object(handle::Int) = SharpObject(handle)

    function net_reflect(target::SharpObject, op::Symbol, args...; returnflags = TryConvertToJuliaNative)
        arg_arr = JulianArg[ifelse(x isa JulianArg, x, JulianArg(x)) for x in args]
        ccall(_netreflect[], Any, (Int, Symbol, Ptr{JulianArg}, Int, ArgFlags), handle(target), op, pointer(arg_arr), length(arg_arr), returnflags)
    end

    function init_sharp_fcns(vars::Vector)
        _sharpfinalizer[] = vars[1]
        _netreflect[] = vars[2]
        _nettostring[] = vars[4]
        _nettohash[] = vars[5]
        _nettotype[] = vars[6]
		
        eval(quote 
                const NetNameResolver = $(vars[3]) 
                const DefaultResolver = NetNameResolver()
				
				usingasm(; asm_name="System.Runtime.InteropServices")
				const MarshalTy = usingname("System.Runtime.InteropServices.Marshal")
				const SharpInteropTy = usingname("JuliaDotNet.SharpInterop")
            end)
    end

    function get_backtrace_str(@nospecialize(ex))
        io = IOBuffer()
        Base.display_error(io, ex, Base.catch_backtrace())
        return String(take!(io))
    end

    function unroot_object_from_sharp(id::Int)
        lock(__roots_lock) do
            delete!(__object_roots, id)
        end
        return nothing
    end

    function root_object_from_sharp(@nospecialize(x))
        # Atomic increment is thread-safe and lock-free
        id = Threads.atomic_add!(__next_id, 1)
                                                  
        lock(__roots_lock) do
             __object_roots[id] = x
        end

        return id
    end

    @generated function compile_delegate(ptr::Ptr{Cvoid}, rt::Type, arg_types...)
        return quote
             ($(args...),) -> ccall(ptr, $rt, ($(arg_types...),), $(args...))
        end
    end
 
    function get_array_ptr_void(@nospecialize(x), len::Ptr{Int})
        unsafe_store!(len, length(x))
        return Ptr{Cvoid}(pointer(x))
    end
 
    function iterate_for_sharp(@nospecialize(x), state_ptr::Ptr{Any})
         state = unsafe_load(state_ptr)
         currAndState = (state === nothing ? Base.iterate(x) : Base.iterate(x, state))
         if currAndState === nothing
             unsafe_store!(state_ptr, nothing)
             return nothing
         end
         unsafe_store!(state_ptr, currAndState[2])
         return currAndState[1]
    end   

    _hresult(x::Cint) = reinterpret(UInt32, x)

    Base.@enum HostFxrStatusCode::UInt32 begin 
        # Success
        Success                             = 0            # Operation was successful
        Success_HostAlreadyInitialized      = 0x00000001   # Initialization was successful, but another host context is already initialized
        Success_DifferentRuntimeProperties  = 0x00000002   # Initialization was successful, but another host context is already initialized and the requested context specified runtime properties which are not the same

        # Failure
        InvalidArgFailure                   = 0x80008081   # One or more arguments are invalid
        CoreHostLibLoadFailure              = 0x80008082   # Failed to load a hosting component
        CoreHostLibMissingFailure           = 0x80008083   # One of the hosting components is missing
        CoreHostEntryPointFailure           = 0x80008084   # One of the hosting components is missing a required entry point
        CurrentHostFindFailure              = 0x80008085   # Failed to get the path of the current hosting component and determine the .NET installation location
        # unused                           = 0x80008086,
        CoreClrResolveFailure               = 0x80008087   # The `coreclr` library could not be found
        CoreClrBindFailure                  = 0x80008088   # Failed to load the `coreclr` library or finding one of the required entry points
        CoreClrInitFailure                  = 0x80008089   # Call to `coreclr_initialize` failed
        CoreClrExeFailure                   = 0x8000808a   # Call to `coreclr_execute_assembly` failed
        ResolverInitFailure                 = 0x8000808b   # Initialization of the `hostpolicy` dependency resolver failed
        ResolverResolveFailure              = 0x8000808c   # Resolution of dependencies in `hostpolicy` failed
        # unused                           = 0x8000808d,
        LibHostInitFailure                  = 0x8000808e   # Initialization of the `hostpolicy` library failed
        # unused                           = 0x8000808f,
        # unused                           = 0x80008090,
        # unused                           = 0x80008091,
        LibHostInvalidArgs                  = 0x80008092   # Arguments to `hostpolicy` are invalid
        InvalidConfigFile                   = 0x80008093   # The `.runtimeconfig.json` file is invalid
        AppArgNotRunnable                   = 0x80008094   # [internal usage only]
        AppHostExeNotBoundFailure           = 0x80008095   # `apphost` failed to determine which application to run
        FrameworkMissingFailure             = 0x80008096   # Failed to find a compatible framework version
        HostApiFailed                       = 0x80008097   # Host command failed
        HostApiBufferTooSmall               = 0x80008098   # Buffer provided to a host API is too small to fit the requested value
        # unused                           = 0x80008099,
        AppPathFindFailure                  = 0x8000809a   # Application path imprinted in `apphost` doesn't exist
        SdkResolveFailure                   = 0x8000809b   # Failed to find the requested SDK
        FrameworkCompatFailure              = 0x8000809c   # Application has multiple references to the same framework which are not compatible
        FrameworkCompatRetry                = 0x8000809d   # [internal usage only]
        # unused                           = 0x8000809e,
        BundleExtractionFailure             = 0x8000809f   # Error extracting single-file bundle
        BundleExtractionIOError             = 0x800080a0   # Error reading or writing files during single-file bundle extraction
        LibHostDuplicateProperty            = 0x800080a1   # The application's `.runtimeconfig.json` contains a runtime property which is produced by the hosting layer
        HostApiUnsupportedVersion           = 0x800080a2   # Feature which requires certain version of the hosting layer was used on a version which doesn't support it
        HostInvalidState                    = 0x800080a3   # Current state is incompatible with the requested operation
        HostPropertyNotFound                = 0x800080a4   # Property requested by `hostfxr_get_runtime_property_value` doesn't exist
        HostIncompatibleConfig              = 0x800080a5   # Host configuration is incompatible with existing host context
        HostApiUnsupportedScenario          = 0x800080a6   # Hosting API does not support the requested scenario
        HostFeatureDisabled                 = 0x800080a7   # Support for a requested feature is disabled
    end

    Base.@enum hostfxr_delegate_type::Int32 begin 
        hdt_com_activation
        hdt_load_in_memory_assembly
        hdt_winrt_activation
        hdt_com_register
        hdt_com_unregister
        hdt_load_assembly_and_get_function_pointer
        hdt_get_function_pointer
        hdt_load_assembly
        hdt_load_assembly_bytes
    end

    function check_code_success(x::Cint, action="")
        if x < 0
            println("HResult:$x")
            error("Host Fxr Error: $(HostFxrStatusCode(_hresult(x))) during $action")
        end
    end

    """
        load_net() -> Bool
    
    Initializes the .NET Host (hostfxr), loads the JuliaDotNet.dll bridge,
    and populates internal function pointers.
    """
    function load_net()
        root = abspath(dirname(dirname(pathof(JuliaDotNet))))
        artifacts_dir = joinpath(root, "artifacts")
        net_output_dir = joinpath(root, "artifacts", "net9.0")

        #ENV["COREHOST_TRACE"] = 1

        #Open nethost to resolve the hostfxr
        nethost_path = joinpath(artifacts_dir, "nethost")
        libnethost = dlopen(nethost_path)
        get_hostfxr_path = dlsym(libnethost, :get_hostfxr_path)
        buffer = Vector{UInt16}(undef, 512)
        buffer_size = Ref{Csize_t}(length(buffer))
        check_code_success(ccall(get_hostfxr_path, Cint, (Ptr{UInt16}, Ptr{Csize_t}, Ptr{Cvoid}), 
                        buffer, buffer_size, C_NULL), "Loading hostfxr") 
        hostfxr_path = transcode(String, buffer[1:buffer_size[]-1])

        libhostfxr = dlopen(hostfxr_path) 

        config_path = joinpath(net_output_dir, "JuliaDotNet.runtimeconfig.json")
        init_fptr = dlsym(libhostfxr, :hostfxr_initialize_for_runtime_config)
        handle = Ref{Ptr{Cvoid}}(C_NULL)
        check_code_success(ccall(init_fptr, Cint, (Cwstring, Ptr{Cvoid}, Ref{Ptr{Cvoid}}), 
               config_path, C_NULL, handle), "hostfxr_initialize_for_runtime_config")
       
        get_delegate_fptr = dlsym(libhostfxr, :hostfxr_get_runtime_delegate)
        load_assembly_ptr = Ref{Ptr{Cvoid}}(C_NULL)
        check_code_success(ccall(get_delegate_fptr, Cint, (Ptr{Cvoid}, Cint, Ptr{Ptr{Cvoid}}), 
               handle[], hdt_load_assembly_and_get_function_pointer, load_assembly_ptr), "Loading hdt_load_assembly_and_get_function_pointer")
                   
        initFromNative_fcn = Ref{Ptr{Cvoid}}(C_NULL)
        assembly_path = joinpath(net_output_dir, "JuliaDotNet.dll")
        type_name = "JuliaDotNet.Julia, JuliaDotNet"
        method_name = "InitFromNative"
    
        UNMANAGEDCALLERSONLY_METHOD = Ptr{Cvoid}(-1)        #coreclr_delegates.h
        check_code_success(ccall(load_assembly_ptr[], Cint, 
          (Cwstring, Cwstring, Cwstring, Ptr{Cvoid}, Ptr{Cvoid}, Ptr{Ptr{Cvoid}}),
               assembly_path, type_name, method_name, UNMANAGEDCALLERSONLY_METHOD, C_NULL, initFromNative_fcn), "Executing hdt_load_assembly_and_get_function_pointer")
    
		result = ccall(initFromNative_fcn[], Cint, ())
		if result != 43
			error(".NET did not load properly! Returned:$result")
			return false
		end
		
        return true
    end
    #Loading .NET CORE
    
end
