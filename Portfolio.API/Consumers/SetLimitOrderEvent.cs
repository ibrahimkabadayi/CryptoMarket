using AutoMapper;
using MassTransit;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Enums;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class SetLimitOrderEvent(ILimitOrderService limitOrderService, IMapper mapper) : IConsumer<LimitOrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<LimitOrderPlacedEvent> context)
    {
        var dto = mapper.Map<CreateLimitOrderDto>(context.Message);        
        await limitOrderService.CreateLimitOrderAsync(dto, context.Message.CorrelationId);
    }
}
