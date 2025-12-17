using TestAttarClone.SharedKernel.BaseTypes;
using MediatR;

namespace TestAttarClone.Application.Abstractions.Messaging;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

public interface ICommand : IRequest<Result>;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
