using System.Collections;
using System.Reflection;

namespace GignerEngine.DiContainer
{
    public class Definition
    {
        internal Bind bind;
        internal object? instance;

        public bool HasTag(string tag)
        {
            return bind.tags.Contains(tag);
        }
    }

    public class DiContainer : IDiContainer
    {
        private Dictionary<Type, Definition> _definitions = new();
        private Dictionary<string, object> _parameters = new();

        public void Apply(DiBuilder builder)
        {
            foreach (var bindRaw in builder.binds)
            {
                var bind = (Bind)bindRaw;
                var reg = new Definition
                {
                    bind = bind
                };

                _definitions.Add(bind.type, reg);
            }

            foreach (var (key, value) in builder.parameters)
            {
                _parameters.Add(key, value);
            }
        }

        public void Init()
        {
            Dictionary<Type, Definition> definitionsToAdd = new();
            foreach (var (type, reg) in _definitions)
            {
                if (reg.bind.factory != null)
                {
                    var factoryReg = new Definition
                    {
                        bind = new Bind
                        {
                            type = reg.bind.factory,
                            instanceType = reg.bind.factory,
                        }
                    };

                    definitionsToAdd.Add(reg.bind.factory, factoryReg);
                } 
            }
            
            foreach (var (type, definition) in definitionsToAdd)
            {
                _definitions.Add(type, definition);
            }

            foreach (var (type, reg) in _definitions)
            {
                if (reg.bind.eager)
                {
                    reg.instance = reg.bind.factory == null ? reg.bind.factoryDelegate == null ? ResolveBind(reg.bind) : reg.bind.factoryDelegate() : ResolveFromFactory(reg.bind);
                    reg.bind.AfterInitDelegates?.Invoke(reg.instance, this);
                }
            }
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public T[] ResolveAll<T>()
        {
            List<T> result = [];
            foreach (var (type, _) in _definitions)
            {
                var interfaces = type.GetInterfaces();
                if (interfaces.Contains(typeof(T))) 
                {
                    result.Add((T)Resolve(type));
                }
            }

            return result.ToArray();
        }

        public object[] ResolveAll(Type type)
        {
            List<object> result = [];
            foreach (var (defType, _) in _definitions)
            {
                if (defType.IsGenericType && defType.GetGenericTypeDefinition() == type)
                {
                    result.Add(Resolve(defType));
                    continue;
                }
                var interfaces = defType.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == type)
                    {
                        result.Add(Resolve(defType));
                        break;
                    }
                }
            }

            return result.ToArray();
        }

        private static List<Type> _circuitProtection = new();
        public object Resolve(Type type)
        {
            try
            {
                if (_circuitProtection.Contains(type))
                {
                    throw new Exception($"Circuit reference on resolve {type.FullName}.");
                }

                _circuitProtection.Add(type);

                if (_definitions.TryGetValue(type, out var definition))
                {
                    if (definition.instance != null)
                    {
                        return definition.instance!;
                    }

                    var obj = ResolveBind(definition.bind);
                    definition.bind.AfterInitDelegates?.Invoke(obj, this);
                    if (definition.bind.asSingle)
                    {
                        definition.instance = obj;
                    }

                    return obj;
                }

                return ResolveUnregistered(type);
            }
            finally
            {
                _circuitProtection = new();
            }
        }
        
        private object ResolveBind(Bind bind)
        {
            try
            {
                if (bind.factory == null && bind.factoryDelegate == null)
                {
                    return ResolveBindWithReflection(bind);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Can't resolve {bind.type}", e);
            }

            return ResolveFromFactory(bind);
        }

        private object ResolveFromFactory(Bind bind)
        {
            if (bind.factory == null)
            {
                return bind.factoryDelegate();
            }
            var factory = (IFactory)Resolve(bind.factory);
            return factory.Create();
        }

        private object ResolveUnregistered(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
                throw new InvalidOperationException($"Can't resolve {type.FullName}");

            var bind = new Bind { instanceType = type, type = type };

            try
            {
                return ResolveBindWithReflection(bind);
            }
            catch (Exception e) when (e is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Can't resolve {type.FullName}", e);
            }
        }

        private object ResolveBindWithReflection(Bind bind)
        {
            ConstructorInfo? constructor = bind.instanceType
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
            {
                var instance = Activator.CreateInstance(bind.instanceType);
                if (instance == null)
                    throw new InvalidOperationException($"Activator.CreateInstance returned null for {bind.instanceType.FullName}");
                return instance;
            }
            var args = new object[constructor.GetParameters().Length];

            int i = 0;
            foreach (var parameterInfo in constructor.GetParameters())
            {
                if (!IsSimpleType(parameterInfo.ParameterType))
                {
                    var parameterType = parameterInfo.ParameterType;
                    if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        var elementType = parameterType.GetGenericArguments()[0];
                        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

                        foreach (var type in _definitions.Keys.Where(type => elementType.IsAssignableFrom(type)))
                        {
                            list.Add(Resolve(type));
                        }

                        args[i] = list;
                    }
                    else
                    {
                        args[i] = Resolve(parameterInfo.ParameterType);
                    }
                }
                else
                {
                    if (!_parameters.TryGetValue(parameterInfo.Name!, out var parameter))
                        throw new InvalidOperationException($"Can't resolve parameter {parameterInfo.Name}. No value was registered.");
                    args[i] = parameter;
                }

                i++;
            }

            var obj = constructor.Invoke(args);
            if (obj == null)
                throw new InvalidOperationException($"Activator.CreateInstance returned null for {bind.instanceType.FullName}");

            return obj;
        }

        public T[] ResolveByTag<T>(string tag)
        {
            List<T> result = new List<T>();
            foreach (var (type, definition) in _definitions)
            {
                if (definition.HasTag(tag))
                {
                    result.Add((T)Resolve(type));
                }
            }

            return result.ToArray();
        }
    
        public static bool IsSimpleType(Type type)
        {
            if (type.IsEnum)
                return true;

            return type == typeof(int)
                   || type == typeof(float)
                   || type == typeof(bool)
                   || type == typeof(double)
                   || type == typeof(Guid)
                   || type == typeof(char)
                   || type == typeof(byte)
                   || type == typeof(string);
        }
    }
}
