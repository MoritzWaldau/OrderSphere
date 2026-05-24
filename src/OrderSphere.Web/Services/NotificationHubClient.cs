using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace OrderSphere.Web.Services;

public sealed class NotificationHubClient : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly NavigationManager _navigation;
    private bool _started;

    public event Action<NotificationMessage>? OnNotificationReceived;

    public NotificationHubClient(NavigationManager navigation)
    {
        _navigation = navigation;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started) return;

        _connection = new HubConnectionBuilder()
            .WithUrl(_navigation.ToAbsoluteUri("/hubs/notifications"))
            .WithAutomaticReconnect()
            .Build();

        _connection.On<NotificationMessage>("ReceiveNotification", message =>
        {
            OnNotificationReceived?.Invoke(message);
        });

        await _connection.StartAsync(ct);
        _started = true;
    }

    public async Task StopAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
            _started = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}

public sealed record NotificationMessage(
    string Type,
    string Title,
    string Message,
    Guid? OrderId,
    DateTime CreatedAt);
