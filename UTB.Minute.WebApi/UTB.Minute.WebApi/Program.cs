using System.Collections.Concurrent;
using System.Text.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
            builder.Services.AddSingleton<SseService>();

            var app = builder.Build();

            app.UseCors();

            // In-memory sample data
            var meals = new[] {
                new MealDto(Guid.NewGuid(), "Špagety Boloňské", "Tradiční špagety s masovou omáčkou.", 89m, true),
                new MealDto(Guid.NewGuid(), "Smažený sýr", "Smažený eidam s hranolkami.", 79m, true),
            };

            var menu = new List<MenuItemDto> {
                new MenuItemDto(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), 10, meals[0]),
                new MenuItemDto(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), 5, meals[1])
            };

            var orders = new ConcurrentDictionary<Guid, OrderDto>();
            var sse = app.Services.GetRequiredService<SseService>();

            app.MapGet("/menu", () => Results.Json(menu));

            app.MapGet("/orders", () => Results.Json(orders.Values));

            app.MapPost("/orders", (CreateOrderDto dto) =>
            {
                var id = Guid.NewGuid();
                var order = new OrderDto(id, dto.MenuItemId, dto.StudentId, OrderStateDto.Preparing);
                orders.TryAdd(id, order);
                sse.Broadcast(order);
                return Results.Created($"/orders/{id}", order);
            });

            app.MapPatch("/orders/{id}/state", (Guid id, ChangeOrderStateDto change) =>
            {
                if (!orders.TryGetValue(id, out var existing)) return Results.NotFound();
                var updated = existing with { State = change.NewState };
                orders[id] = updated;
                sse.Broadcast(updated);
                return Results.Ok(updated);
            });

            app.MapGet("/orders/sse", async (HttpContext ctx) =>
            {
                ctx.Response.Headers.Add("Cache-Control", "no-cache");
                ctx.Response.Headers.Add("Content-Type", "text/event-stream");
                await sse.AddClientAsync(ctx.Response);
            });

            app.Run();
        }
    }

    public class SseService
    {
        private readonly List<HttpResponse> clients = new();
        private readonly List<System.Threading.Channels.Channel<OrderDto>> subscribers = new();
        private readonly object sync = new();

        public async Task AddClientAsync(HttpResponse response)
        {
            lock (sync) clients.Add(response);
            try
            {
                // keep the connection open
                await response.Body.FlushAsync();
                var tcs = new TaskCompletionSource<object?>();
                await tcs.Task; // never completes until aborted
            }
            catch { }
            finally
            {
                lock (sync) clients.Remove(response);
            }
        }

        // Called by other parts of the app to notify about new orders
        public Task NotifyOrderUpdate(OrderDto order)
        {
            Broadcast(order);
            // also push to channel subscribers
            lock (sync)
            {
                foreach (var ch in subscribers.ToArray())
                {
                    _ = ch.Writer.TryWrite(order);
                }
            }
            return Task.CompletedTask;
        }

        public Task NotifyOrderUpdate(CreateOrderDto dto)
        {
            var order = new OrderDto(Guid.NewGuid(), dto.MenuItemId, dto.StudentId, OrderStateDto.Preparing);
            return NotifyOrderUpdate(order);
        }

        public System.Collections.Generic.IAsyncEnumerable<OrderDto> Subscribe(System.Threading.CancellationToken ct)
        {
            var ch = System.Threading.Channels.Channel.CreateUnbounded<OrderDto>();
            lock (sync) subscribers.Add(ch);

            return ReadAllAsync(ch.Reader, ch, ct);
        }

        private async IAsyncEnumerable<OrderDto> ReadAllAsync(System.Threading.Channels.ChannelReader<OrderDto> reader, System.Threading.Channels.Channel<OrderDto> ch, [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken ct)
        {
            try
            {
                while (await reader.WaitToReadAsync(ct))
                {
                    while (reader.TryRead(out var item))
                    {
                        yield return item;
                    }
                }
            }
            finally
            {
                lock (sync) subscribers.Remove(ch);
            }
        }

        public void Broadcast(OrderDto order)
        {
            var json = JsonSerializer.Serialize(order);
            var payload = $"data: {json}\n\n";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(payload);
            lock (sync)
            {
                foreach (var client in clients.ToArray())
                {
                    try
                    {
                        client.Body.Write(bytes, 0, bytes.Length);
                        client.Body.Flush();
                    }
                    catch
                    {
                        // ignore write errors; removal happens elsewhere
                    }
                }
            }
        }
    }
}
