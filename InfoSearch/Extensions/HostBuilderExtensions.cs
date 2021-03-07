using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InfoSearch.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseStartup<TStartup>(this IHostBuilder hostBuilder) where TStartup : class
        {
            hostBuilder.ConfigureServices((ctx, serviceCollection) =>
            {
                var cfgServicesMethod = typeof(TStartup).GetMethod("ConfigureServices", new[] { typeof(IServiceCollection) });

                var configCtor = typeof(TStartup).GetConstructor(new[] { typeof(IConfiguration) });

                var startUpObj = New<TStartup>(configCtor, ctx.Configuration);

                cfgServicesMethod?.Invoke(startUpObj, new object[] { serviceCollection });
            });

            return hostBuilder;
        }
		
        private static T New<T>(ConstructorInfo ctor, params object[] argsObj)
        {
            if (ctor == default)
                return default;

            var parameters = ctor.GetParameters();
            var args = new Expression[parameters.Length];
            var parameter = Expression.Parameter(typeof(object[]));
			
            for (var i = 0; i != parameters.Length; ++i)
            {
                args[i] = Expression.Convert(Expression.ArrayIndex(parameter, Expression.Constant(i)), parameters[i].ParameterType);
            }
			
            var expression = Expression.Lambda<Func<object[], T>>(Expression.New(ctor, args), parameter);
            var func = expression.Compile();
            return func(argsObj);
        }
    }
}