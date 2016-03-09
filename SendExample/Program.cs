using System;
using MediatR;
using StructureMap;
using StructureMap.Graph;

namespace SendExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var mediator = CreateMediator();

            var response = mediator.Send(new Ping());

            Console.WriteLine(response);

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
}
