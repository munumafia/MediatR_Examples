using System;
using System.Threading.Tasks;
using MediatR;
using StructureMap;
using StructureMap.Graph;

namespace AsyncExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var mediator = CreateMediator();

            var response = mediator.SendAsync(new Ping());

            Task.WaitAll(response);

            Console.WriteLine(response.Result);

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

    public class Ping : IAsyncRequest<string>
    {
    }

    public class PingHandler : IAsyncRequestHandler<Ping, string>
    {
        public async Task<string> Handle(Ping message)
        {
            return await Task.Factory.StartNew(() => "Pong");
        }
    }
}
