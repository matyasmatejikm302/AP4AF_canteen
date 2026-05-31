using System.Threading.Channels;
using UTB.Minute.Contracts;

namespace UTB.Minute.WebApi.Services;

public class SseService
{
    private readonly Channel<OrderDto> _channel = Channel.CreateUnbounded<OrderDto>();

    public async Task NotifyOrderUpdate(OrderDto order) => await _channel.Writer.WriteAsync(order);

    public IAsyncEnumerable<OrderDto> Subscribe(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}