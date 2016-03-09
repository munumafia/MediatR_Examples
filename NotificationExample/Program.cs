using System;
using MediatR;
using StructureMap;
using StructureMap.Graph;

namespace NotificationExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var mediator = CreateMediator();

            mediator.Publish(new Ping());

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

    public class Ping : INotification
    {
    }

    public class PingNotificationHandler1 : INotificationHandler<Ping>
    {
        public void Handle(Ping notification)
        {
            Console.WriteLine("Pong from PingNotificationHandler1.");
        }
    }

    public class PingNotificationHandler2 : INotificationHandler<Ping>
    {
        public void Handle(Ping notification)
        {
            Console.WriteLine("Pong from PingNotificationHandler2.");
        }
    }
}
