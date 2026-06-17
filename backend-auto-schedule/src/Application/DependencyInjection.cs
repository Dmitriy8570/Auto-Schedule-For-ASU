using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Application.Common.Behaviors;
using Application.Solver.Mapping;
using Application.Solver.Solving;
using FluentValidation;
using MediatR;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Солвер расписания (без состояния).
        services.AddSingleton<IScheduleSolver, ScheduleSolver>();
        services.AddSingleton<IScheduleResultMapper, ScheduleResultMapper>();

        return services;
    }
}