using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediatR;
using StructureMap;
using StructureMap.Graph;

namespace DecoratorExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var mediator = CreateMediator();

            var response = mediator.Send(new Ping());
            if (response.CommandResultType != CommandResultType.Success)
            {
                response.ValidationErrors.ToList().ForEach(Console.WriteLine);
            }

            Console.WriteLine(response.Entity);

            Console.ReadLine();

            var response2 = mediator.Send(new Ping2());

            Console.WriteLine(response2);

            Console.ReadLine();
        }

        private static IMediator CreateMediator()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.TheCallingAssembly();
                    scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
                    scanner.ConnectImplementationsToTypesClosing(typeof(ICommandHandler<,>));
                    scanner.ConnectImplementationsToTypesClosing(typeof(IAsyncRequestHandler<,>));
                    scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
                    scanner.ConnectImplementationsToTypesClosing(typeof(IAsyncNotificationHandler<>));
                    scanner.ConnectImplementationsToTypesClosing(typeof(IValidator<>));
                });
                cfg.For<SingleInstanceFactory>().Use<SingleInstanceFactory>(ctx => t => ctx.GetInstance(t));
                cfg.For<MultiInstanceFactory>().Use<MultiInstanceFactory>(ctx => t => ctx.GetAllInstances(t));
                cfg.For<IMediator>().Use<Mediator>();
                cfg.For(typeof(IRequestHandler<,>)).DecorateAllWith(typeof(LoggingHandler<,>));
                
                Assembly.GetExecutingAssembly().GetTypes()
                    .Where(type => type.Name != typeof(ValidationHandler<,>).Name)
                    .Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
                    .ToList()
                    .ForEach(type =>
                    {
                        var iface = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
                        var message = iface.GetTypeInfo().GenericTypeArguments[0];
                        var returnType = iface.GetTypeInfo().GenericTypeArguments[1];
                        var commandType = typeof(CommandResult<>).MakeGenericType(returnType);

                        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(message, commandType);
                        var wrapperType = typeof(ValidationHandler<,>).MakeGenericType(message, returnType);

                        cfg.For(handlerType).DecorateAllWith(wrapperType);
                    });
            });

            

            return container.GetInstance<IMediator>();
        }
    }

    public class Ping : IRequest<CommandResult<string>>
    {
        public string Message { get; set; }
    }

    public class PingHandler : ICommandHandler<Ping, string>
    {
        public CommandResult<string> Handle(Ping message)
        {
            return new CommandResult<string>
            {
                Entity = "Pong"
            };
        }
    }

    public interface ICommand<TResponse> : IRequest<CommandResult<TResponse>>
    {
        
    }

    public interface ICommandHandler<in TRequest, TResponse> : IRequestHandler<TRequest, CommandResult<TResponse>> where TRequest : IRequest<CommandResult<TResponse>>

    {
        
    }
    

    public class Ping2 : IRequest<string>
    {
    }

    public class Ping2Handler : IRequestHandler<Ping2, string>
    {
        public string Handle(Ping2 message)
        {
            return "Pong2";
        }
    }

    public enum CommandResultType
    {
        Success,
        Error,
        ValidationError
    }

    public class CommandResult<TEntity>
    {
        public CommandResultType CommandResultType { get; set; } = CommandResultType.Success;

        public TEntity Entity { get; set; }

        public IList<string> ValidationErrors { get; set; } = new List<string>();
    }

    public class ValidationHandler<TRequest, TResponse> : ICommandHandler<TRequest, TResponse> 
        where TRequest : IRequest<CommandResult<TResponse>>
    {
        private readonly ICommandHandler<TRequest, TResponse> _Decorated;
        private readonly IList<IValidator<TRequest>> _Validators;

        public ValidationHandler(ICommandHandler<TRequest, TResponse> decorated, IList<IValidator<TRequest>> validators)
        {
            _Decorated = decorated;
            _Validators = validators;
        }

        public CommandResult<TResponse> Handle(TRequest message)
        {
            var validationErrors = new List<string>();
            _Validators.ToList().ForEach(validator => validationErrors.AddRange(validator.Validate(message)));

            if (validationErrors.Any())
            {
                return new CommandResult<TResponse>()
                {
                    CommandResultType = CommandResultType.ValidationError,
                    ValidationErrors = validationErrors
                };
            }

            return _Decorated.Handle(message);
        }
    }

    public interface IValidator<in TCommand>
    {
        IList<string> Validate(TCommand command);
    }

    public class PingCommandValidatator : IValidator<Ping>
    {
        public IList<string> Validate(Ping command)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(command.Message))
            {
                errors.Add("The Message parameter cannot be null or empty");
            }

            if (!string.IsNullOrEmpty(command.Message) && command.Message != "Ping")
            {
                errors.Add($@"Protocol violation, received ""{command.Message}"" for Message parameter instead of ""Ping""");
            }

            return errors;
        }
    }

    public class LoggingHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _DecoratedRequest;

        public LoggingHandler(IRequestHandler<TRequest, TResponse> decoratedRequest)
        {
            _DecoratedRequest = decoratedRequest;
        }

        public TResponse Handle(TRequest message)
        {
            Console.WriteLine("Logging Request: {0}", message);

            var response = _DecoratedRequest.Handle(message);

            Console.WriteLine("Logging Response: {0}", response);

            return response;
        }
    }

    public class SpecialHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _DecoratedRequest;

        public SpecialHandler(IRequestHandler<TRequest, TResponse> decoratedRequest)
        {
            _DecoratedRequest = decoratedRequest;
        }

        public TResponse Handle(TRequest message)
        {
            Console.WriteLine("Handling from special handler.");

            return _DecoratedRequest.Handle(message);
        }
    }
}
