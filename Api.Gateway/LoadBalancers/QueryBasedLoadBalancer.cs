using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.LoadBalancers;


/// <summary>
/// Балансировщик нагрузки на основе параметра запроса <c>id</c>.
/// </summary>
/// <remarks>
/// Алгоритм: индекс реплики = id % количество_реплик.
/// Это гарантирует, что один и тот же идентификатор всегда попадает
/// на одну и ту же реплику (sticky routing по id).
/// </remarks>
/// <param name="serviceProviderFactory">
/// Делегат, возвращающий список доступных реплик сервиса.
/// Вызывается при каждом запросе, чтобы учитывать динамически
/// изменяющийся пул экземпляров.
/// </param>
public class QueryBasedLoadBalancer(Func<Task<List<Service>>> serviceProviderFactory) : ILoadBalancer
{
    public string Type => nameof(QueryBasedLoadBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var services = await serviceProviderFactory();

        if (services.Count == 0)
            throw new InvalidOperationException("No available downstream services");

        if (!context.Request.Query.TryGetValue("id", out var idValue) ||
            !int.TryParse(idValue, out var id))
            throw new InvalidOperationException("Query parameter 'id' is missing or invalid");

        var index = Math.Abs(id) % services.Count;

        return new OkResponse<ServiceHostAndPort>(services[index].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}