namespace GignerEngine.DiContainer;

public interface IFactory
{
    public object Create();
}
public interface IFactory<T> : IFactory
{
    public new T Create();
}