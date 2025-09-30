// Self-contained DI extension without relying on extension methods (manual registration)
namespace Wysg.Musm.Infrastructure.AI;

using Wysg.Musm.Domain.AI;
using Wysg.Musm.Infrastructure.AI.NoOp;
using Wysg.Musm.UseCases.AI;
using Wysg.Musm.Infrastructure.AI.Ollama;
using System.Net.Http;

public static class ServiceCollectionExtensions
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMusmAi(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, bool useNoOp = true, OllamaOptions? ollamaOptions = null)
    {
        void AddSingleton<TService, TImpl>() where TImpl : class, TService where TService : class =>
            services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(TService), typeof(TImpl), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton));
        void AddSingletonInstance<TService>(TService inst) where TService : class =>
            services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(TService), inst));
        void AddScoped<TService, TImpl>() where TImpl : class, TService where TService : class =>
            services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(TService), typeof(TImpl), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped));

        AddScoped<IReportPipeline, ReportPipeline>();

        // Basic HttpClient + simple factory
        AddSingletonInstance<IHttpClientFactory>(new SimpleHttpClientFactory());

        if (useNoOp)
        {
            AddSingleton<IStudyRemarkParser, NoOpStudyRemarkParser>();
            AddSingleton<IPatientRemarkParser, NoOpPatientRemarkParser>();
            AddSingleton<IConclusionGenerator, NoOpConclusionGenerator>();
            AddSingleton<IProofreader, NoOpProofreader>();
            AddSingleton<ILLMClient, NoOpLlmClient>();
            AddSingleton<IModelRouter, NoOpModelRouter>();
            AddSingleton<IInferenceTelemetry, NoOpInferenceTelemetry>();
        }
        else
        {
            var opts = ollamaOptions ?? new OllamaOptions();
            AddSingletonInstance(opts);
            AddSingleton<ILLMClient, OllamaLlmClient>();
            AddSingleton<IModelRouter, OllamaModelRouter>();
            AddSingleton<IInferenceTelemetry, NoOpInferenceTelemetry>(); // placeholder
            // Skills still NoOp until real parsing/JSON logic implemented
            AddSingleton<IStudyRemarkParser, NoOpStudyRemarkParser>();
            AddSingleton<IPatientRemarkParser, NoOpPatientRemarkParser>();
            AddSingleton<IConclusionGenerator, NoOpConclusionGenerator>();
            AddSingleton<IProofreader, NoOpProofreader>();
        }
        return services;
    }

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}
