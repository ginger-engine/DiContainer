using System.Reflection;
using Xunit;

namespace GignerEngine.DiContainer.Tests
{
    public class DiContainerTests
    {
        private class Foo {}

        private class Bar
        {
            public Foo Foo { get; }
            public Bar(Foo foo)
            {
                Foo = foo;
            }
        }

        private interface IService;

        private class Service1 : IService;

        private class Service2 : IService;

        private class ParameterClass
        {
            public string Name { get; }
            public ParameterClass(string name)
            {
                Name = name;
            }
        }

        private class FooFactory : IFactory<Foo>
        {
            public Foo Create() => new Foo();
            object IFactory.Create() => Create();
        }

        private static DiContainer CreateContainer(IEnumerable<Bind> binds, Dictionary<string, object>? parameters = null)
        {
            var container = new DiContainer();
            var defs = new Dictionary<System.Type, Definition>();
            foreach (var bind in binds)
            {
                defs.Add(bind.type, new Definition { bind = bind });
            }
            typeof(DiContainer).GetField("_definitions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(container, defs);
            typeof(DiContainer).GetField("_parameters", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(container, parameters ?? new());
            container.Init();
            return container;
        }

        [Fact]
        public void Resolve_ReturnsSameInstance_WhenAsSingle()
        {
            var container = CreateContainer(new[] { new Bind<Foo>().AsSingle() });

            var foo1 = container.Resolve<Foo>();
            var foo2 = container.Resolve<Foo>();

            Assert.Same(foo1, foo2);
        }

        [Fact]
        public void Resolve_ReturnsDifferentInstances_WhenAsMultiple()
        {
            var container = CreateContainer(new[] { new Bind<Foo>().AsMultiple() });

            var foo1 = container.Resolve<Foo>();
            var foo2 = container.Resolve<Foo>();

            Assert.NotSame(foo1, foo2);
        }

        [Fact]
        public void Resolve_InjectsDependencies()
        {
            var binds = new Bind[] { new Bind<Foo>(), new Bind<Bar>() };
            var container = CreateContainer(binds);

            var bar = container.Resolve<Bar>();

            Assert.NotNull(bar.Foo);
        }

        [Fact]
        public void Resolve_IncludesAllImplementations_WhenUsingResolveAll()
        {
            var binds = new Bind[]
            {
                new Bind<Service1>().AsMultiple(),
                new Bind<Service2>().AsMultiple()
            };
            var container = CreateContainer(binds);

            var services = container.ResolveAll<IService>();

            Assert.Contains(services, s => s is Service1);
            Assert.Contains(services, s => s is Service2);
        }

        [Fact]
        public void Resolve_PassesParameters()
        {
            var parameters = new Dictionary<string, object> { { "name", "test" } };
            var container = CreateContainer(new[] { new Bind<ParameterClass>() }, parameters);

            var obj = container.Resolve<ParameterClass>();

            Assert.Equal("test", obj.Name);
        }

        [Fact]
        public void Resolve_UsingFactory()
        {
            var binds = new[] { new Bind<Foo>().FromFactory<FooFactory>() };
            var container = CreateContainer(binds);

            var foo = container.Resolve<Foo>();

            Assert.NotNull(foo);
        }

        private class AutoDep {}
        private class AutoTarget
        {
            public AutoDep Dep { get; }
            public AutoTarget(AutoDep dep)
            {
                Dep = dep;
            }
        }

        private interface IUnknown {}
        private class NeedInterface
        {
            public NeedInterface(IUnknown p) {}
        }

        [Fact]
        public void Resolve_AutoResolves_UnregisteredType()
        {
            var container = CreateContainer(Array.Empty<Bind>());

            var obj = container.Resolve<AutoTarget>();

            Assert.NotNull(obj);
            Assert.NotNull(obj.Dep);
        }

        [Fact]
        public void Resolve_UnregisteredInterface_Throws()
        {
            var container = CreateContainer(Array.Empty<Bind>());

            Assert.Throws<InvalidOperationException>(() => container.Resolve<NeedInterface>());
        }

        [Fact]
        public void Resolve_UsesRegisteredDependencies_WhenAutoResolving()
        {
            var foo = new Foo();
            var binds = new[] { new Bind<Foo>().FromInstance(foo) };
            var container = CreateContainer(binds);

            var bar = container.Resolve<Bar>();

            Assert.Same(foo, bar.Foo);
        }
    }
}
