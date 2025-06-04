namespace GignerEngine.DiContainer
{
    public interface IReadonlyDiContainer
    {
        public T Resolve<T>();
        public T[] ResolveAll<T>();
        public object[] ResolveAll(Type type);
        public object Resolve(Type type);
        public T[] ResolveByTag<T>(string tag);
    }
}