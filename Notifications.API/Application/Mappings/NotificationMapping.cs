using AutoMapper;
using Notifications.API.Application.DTOs;
using Notifications.API.Domain.Entities;

namespace Notifications.API.Application.Mappings;

public class NotificationMapping : Profile
{
    public NotificationMapping()
    {
        CreateMap<Notification, NotificationDto>();

        CreateMap<NotificationDto, Notification>();
    }
}
