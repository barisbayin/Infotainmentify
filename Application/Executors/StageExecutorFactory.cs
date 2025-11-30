using Application.Abstractions;
using Core.Attributes;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.Executors
{
    public class StageExecutorFactory
    {
        private readonly Dictionary<StageType, Type> _map = new();
        private readonly IServiceProvider _sp;

        public StageExecutorFactory(IServiceProvider sp)
        {
            _sp = sp;

            var executorTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IStageExecutor).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in executorTypes)
            {
                var attr = type.GetCustomAttribute<StageExecutorAttribute>();
                if (attr != null)
                    _map[attr.Type] = type;
            }
        }

        public IStageExecutor Resolve(StageType type)
        {
            if (!_map.TryGetValue(type, out var impl))
                throw new NotSupportedException($"Executor yok: {type}");

            return (IStageExecutor)_sp.GetRequiredService(impl);
        }
    }

}
