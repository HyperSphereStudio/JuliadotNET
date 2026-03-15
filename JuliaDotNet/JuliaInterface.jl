module JuliadotNet
    export invokemem, invokememvoid, JulianArg, SharpObject, usingasm, usingname, NetNameResolver, nettypeof

    const __roots_lock = ReentrantLock()
    const __object_roots = Dict{Int64, Any}()
    const __next_id = Threads.Atomic{Int64}(1)
    const _sharpfinalizer = Ref{Ptr{Cvoid}}()
    const _netreflect = Ref{Ptr{Cvoid}}()
    const _nettostring = Ref{Ptr{Cvoid}}()
    const _nettohash = Ref{Ptr{Cvoid}}()
    const _nettotype = Ref{Ptr{Cvoid}}()
    
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

    @enum ArgFlags::Int begin
        Nothing = 0
        TryConvertToSharpNative = 1
        TryConvertToJuliaNative = 2
        DiscardReturn = 4
    end

    struct JulianArg
        x::Any                      #IntPtr
        flags::ArgFlags
        
        JulianArg(x::Any, flags::ArgFlags = TryConvertToSharpNative) = new(x, flags)
    end

    nettypeof(x::SharpObject) = ccall(_nettotype[], Any, (Int, ), handle(x))
    Base.hash(x::SharpObject) = ccall(_nettohash[], Int, (Int, ), handle(x))
    Base.string(x::SharpObject) = ccall(_nettostring[], Any, (Int, ), handle(x))
    Base.print(io::IO, x::SharpObject) = print(io, string(x))
    Base.show(io::IO, x::SharpObject) = show(io, string(x))

    usingasm(x::SharpObject, name::String) = invokemem(x, :UsingAssembly, name)
    usingname(x::SharpObject, name::String) = invokemem(x, :Using, name)
    
    Base.getproperty(x::SharpObject, s::Symbol; returnflags = TryConvertToJuliaNative) = net_reflect(x, :getproperty, s; returnflags = returnflags)
    Base.getproperty(x::JulianArg, s::Symbol) = getproperty(x.x, s; returnflags=x.flags)
    Base.getindex(x::SharpObject, idxs...; returnflags = TryConvertToJuliaNative) = net_reflect(x, :getindex, idxs...; returnflags = returnflags)
    Base.getindex(x::JulianArg, idxs...) = getindex(x.x, idxs...; returnflags=x.flags)
    Base.setindex!(x::SharpObject, v, idxs...; returnflags = TryConvertToJuliaNative) = net_reflect(x, :setindex!, v, idxs...; returnflags = returnflags)
    Base.setindex!(x::JulianArg, v, idxs...) = setindex!(x.x, v, idxs...; returnflags=x.flags)
    
    (x::SharpObject)(args...; returnflags=TryConvertToJuliaNative) = net_reflect(x, :invoke, args...; returnflags = returnflags)
    (x::JulianArg)(args...) = x.x(args...; returnflags=x.flags)
    
    invokemem(x::SharpObject, name::Symbol, args...; returnflags = TryConvertToJuliaNative) = net_reflect(x, :invokeMember, name, args...; returnflags = returnflags)
    invokemem(x::JulianArg, name::Symbol, args...; returnflags = TryConvertToJuliaNative) = invokemem(x.x, name, args...; returnflags=x.flags)
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
        eval(:(const NetNameResolver = $(vars[3])))
        _nettostring[] = vars[4]
        _nettohash[] = vars[5]
        _nettotype[] = vars[6]
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
         currAndState = (state == nothing ? Base.iterate(x) : Base.iterate(x, state))
         if currAndState === nothing
             unsafe_store!(state_ptr, nothing)
             return nothing
         end
         unsafe_store!(state_ptr, currAndState[2])
         return currAndState[1]
     end
    
end
