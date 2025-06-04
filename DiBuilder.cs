namespace GignerEngine.DiContainer
{
    public class Bind
    {
        internal Type type;
        internal Type instanceType;
        internal bool asSingle = true;
        internal bool eager = false;
        internal ICollection<string> tags = new List<string>();
        public delegate object FactoryDelegate();
        internal Type? factory;
        internal FactoryDelegate? factoryDelegate;
        
        public delegate void AfterInitDelegate(object obj, IReadonlyDiContainer diContainer);
        internal AfterInitDelegate? AfterInitDelegates;
    }
    public class Bind<T> : Bind
    {
        public Bind(Type type)
        {
            this.type = type;
        }
        
        public Bind()
        {
            type = typeof(T);
            instanceType = typeof(T);
        }

        public Bind<T> AsSingle()
        {
            asSingle = true;
            return this;
        }

        public Bind<T> AsMultiple()
        {
            asSingle = false;
            return this;
        }

        public Bind<T> Eager()
        {
            eager = true;
            return this;
        }

        public Bind<T> Lazy()
        {
            eager = false;
            return this;
        }

        public Bind<T> From<E>() where E:T
        {
            factory = null;
            instanceType = typeof(E);
            return this;
        }

        public Bind<T> FromInstance(T instance)
        {
            factoryDelegate = () => instance;
            instanceType = typeof(T);
            return this;
        }

        public Bind<T> FromFactory<F>() where F: IFactory<T>
        {
            factory = typeof(F);

            return this;
        }
        
        public Bind<T> FromFactory(FactoryDelegate deleg)
        {
            factoryDelegate = deleg;
            instanceType = typeof(T);
            return this;
        }

        public Bind<T> AddTag(string tag)
        {
            tags.Add(tag);
            return this;
        }

        public Bind<T> AfterInit(AfterInitDelegate afterInitDelegate)
        {
            AfterInitDelegates = afterInitDelegate;
            
            return this;
        }
    }
    
    public class DiBuilder
    {
        internal List<object> binds = new();
        internal Dictionary<string, object> parameters = new();
        
        public Bind<T> Bind<T>()
        {
            var bind = new Bind<T>();
            binds.Add(bind);

            return bind;
        }

        public void BindParameter(string name, object value)
        {
            parameters.Add(name, value);
        }
    }
}