using MediatR;
using System;

namespace VAH.Backend.Models.DomainEvents;

public interface IDomainEvent : INotification
{
}
