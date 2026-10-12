using System.Reflection;

namespace TrainingApp.Api.Common;

public static class DeliveryStrategyRegistration
{
    /// <summary>
    /// Registers every non-abstract class from <paramref name="assembly"/> that implements
    /// <see cref="IDeliveryStrategy{TCommand}"/> as a singleton for that closed interface.
    /// </summary>
    public static IServiceCollection AddDeliveryStrategies(this IServiceCollection services, Assembly assembly)
    {
        var registrations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeliveryStrategy<>))
                .Select(i => (Service: i, Implementation: type)));

        foreach (var group in registrations.GroupBy(r => r.Service))
        {
            // The command type must uniquely determine the strategy.
            if (group.Count() > 1)
            {
                var implementations = string.Join(", ", group.Select(r => r.Implementation.Name));
                throw new InvalidOperationException(
                    $"Several strategies implement {group.Key.Name}<{group.Key.GenericTypeArguments[0].Name}>: {implementations}.");
            }

            services.AddSingleton(group.Key, group.Single().Implementation);
        }

        return services;
    }
}
