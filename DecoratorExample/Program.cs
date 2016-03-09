using System;
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

            Console.WriteLine(response);

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
                    scanner.ConnectImplementationsToTypesClosing(typeof(IAsyncRequestHandler<,>));
                    scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
                    scanner.ConnectImplementationsToTypesClosing(typeof(IAsyncNotificationHandler<>));
                });
                cfg.For<SingleInstanceFactory>().Use<SingleInstanceFactory>(ctx => t => ctx.GetInstance(t));
                cfg.For<MultiInstanceFactory>().Use<MultiInstanceFactory>(ctx => t => ctx.GetAllInstances(t));
                cfg.For<IMediator>().Use<Mediator>();
                cfg.For(typeof(IRequestHandler<,>)).DecorateAllWith(typeof(LoggingHandler<,>));
                cfg.For<IRequestHandler<Ping, string>>().DecorateAllWith<SpecialHandler<Ping, string>>();
            });

            return container.GetInstance<IMediator>();
        }
    }

    public class Ping : IRequest<string>
    {
    }

    public class PingHandler : IRequestHandler<Ping, string>
    {
        public string Handle(Ping message)
        {
            return "Pong";
        }
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
