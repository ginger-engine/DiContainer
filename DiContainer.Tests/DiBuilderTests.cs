using System.Reflection;
using Xunit;

namespace GignerEngine.DiContainer.Tests;

public class DiBuilderTests
{
    private class Foo {}
    private class Bar {}
    private class FooFactory : IFactory<Foo>
    {
        public Foo Create() => new Foo();
        object IFactory.Create() => Create();
    }

    [Fact]
    public void Bind_DefaultsToAsSingle()
    {
        var builder = new DiBuilder();
        var bind = builder.Bind<Foo>();

        var asSingle = (bool)typeof(Bind).GetField("asSingle", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bind)!;
        Assert.True(asSingle);
    }

    [Fact]
    public void AsMultiple_SetsAsSingleFalse()
    {
        var builder = new DiBuilder();
        var bind = builder.Bind<Foo>().AsMultiple();

        var asSingle = (bool)typeof(Bind).GetField("asSingle", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bind)!;
        Assert.False(asSingle);
    }

    [Fact]
    public void EagerAndLazy_TogglesFlag()
    {
        var builder = new DiBuilder();
        var bind = builder.Bind<Foo>().Eager();
        var eager = (bool)typeof(Bind).GetField("eager", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bind)!;
        Assert.True(eager);

        bind.Lazy();
        eager = (bool)typeof(Bind).GetField("eager", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bind)!;
        Assert.False(eager);
    }

    [Fact]
    public void From_SetsInstanceType()
    {
        var builder = new DiBuilder();
        var bind = builder.Bind<Foo>().From<Bar>();

        var instanceType = (Type)typeof(Bind).GetField("instanceType", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bind)!;
        Assert.Equal(typeof(Bar), instanceType);
    }

    [Fact]
    public void FromInstance_UsesFactoryDelegate()
    {
        var builder = new DiBuilder();
        var instance = new Foo();
        var bind = builder.Bind<Foo>().FromInstance(instance);

        var factoryDel = (Bind.FactoryDelegate)typeof(Bind).GetField("factoryDelegate", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bind)!;
        Assert.Same(instance, factoryDel());
    }

    [Fact]
    public void FromFactory_SetsFactoryType()
    {
        var builder = new DiBuilder();
        var bind = builder.Bind<Foo>().FromFactory<FooFactory>();

        var factoryType = (Type)typeof(Bind).GetField("factory", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bind)!;
        Assert.Equal(typeof(FooFactory), factoryType);
    }

    [Fact]
    public void AddTag_StoresTag()
    {
        var builder = new DiBuilder();
        var bind = builder.Bind<Foo>().AddTag("tag1");

        var tags = (ICollection<string>)typeof(Bind).GetField("tags", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bind)!;
        Assert.Contains("tag1", tags);
    }

    [Fact]
    public void BindParameter_AddsParameter()
    {
        var builder = new DiBuilder();
        builder.BindParameter("name", "value");

        var parameters = (Dictionary<string, object>)typeof(DiBuilder).GetField("parameters", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;
        Assert.True(parameters.ContainsKey("name"));
        Assert.Equal("value", parameters["name"]);
    }
}
