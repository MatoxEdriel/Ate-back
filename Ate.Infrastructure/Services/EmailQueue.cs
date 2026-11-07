using System.Threading.Channels;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Enums;

namespace Ate.Infrastructure.Services;

/// <summary>In-memory bounded queue consumed by <see cref="EmailDispatchWorker"/>.</summary>
public sealed class EmailQueue : IEmailQueue
{
    private const int Capacity = 500;

    private readonly Channel<Func<IEmailService, CancellationToken, Task>> _channel =
        Channel.CreateBounded<Func<IEmailService, CancellationToken, Task>>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true
        });

    internal ChannelReader<Func<IEmailService, CancellationToken, Task>> Reader => _channel.Reader;

    public bool TryEnqueueTemplate<TModel>(string toEmail, string subject, EmailTemplateType templateType, TModel model) =>
        _channel.Writer.TryWrite((emailService, cancellationToken) =>
            emailService.SendTemplateEmailAsync(toEmail, subject, templateType, model, cancellationToken: cancellationToken));
}
