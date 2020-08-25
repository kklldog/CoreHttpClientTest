using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreHttpTest
{
    public class MyObserver<T> : IObserver<T>
    {
        private Action<T> _next;
        public MyObserver(Action<T> next)
        {
            _next = next;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(T value) => _next(value);
    }
    public class DisableActivityHandler : DelegatingHandler
    {
        public DisableActivityHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {

        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Activity.Current = null;

            return await base.SendAsync(request, cancellationToken);
        }
    }
    class Program
    {
        private const string EnableActivityPropagationEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION";

        static void Main(string[] args)
        {
            DiagnosticListener.AllListeners.Subscribe(new MyObserver<DiagnosticListener>(listener =>
            {
                //判断发布者的名字
                if (listener.Name == "HttpHandlerDiagnosticListener")
                {
                    //获取订阅信息
                    listener.Subscribe(new MyObserver<KeyValuePair<string, object>>(listenerData =>
                    {
                        System.Console.WriteLine($"监听名称:{listenerData.Key}");
                        dynamic data = listenerData.Value;
                        //打印发布的消息
                        //System.Console.WriteLine($"获取的信息为:{data.Name}的地址是{data.Address}");
                    }));

                    //listener.SubscribeWithAdapter(new MyDiagnosticListener());
                }
            }));

            string switchName = "System.Net.Http.EnableActivityPropagation";
            AppContext.SetSwitch(switchName, false);
            if (AppContext.TryGetSwitch(switchName, out bool sw))
            {
                Console.WriteLine(sw);
            }
            var envVarb = Environment.GetEnvironmentVariable(EnableActivityPropagationEnvironmentVariableSettingName);
            Console.WriteLine(envVarb);
            HttpClient.DefaultProxy = new WebProxy("localhost", 8888);
            //var httpClient = new HttpClient(new DisableActivityHandler(new HttpClientHandler()));
            var httpClient = new HttpClient();

            var result = httpClient.GetAsync("http://localhost").Result;

            Console.WriteLine(result.Content.ReadAsStringAsync().Result);
        }
    }
}
