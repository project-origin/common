using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectOrigin.ServiceCommon.Database.Postgres;

internal class DatabaseConfigurationBuilder : IDatabaseConfigurationBuilder
{
    public IEnumerable<Assembly> DatabaseScriptsAssemblies = new List<Assembly>();
    public IDictionary<Type, ObjectFactory> RepositoryFactories = new Dictionary<Type, ObjectFactory>();

    public void AddRepository<TService, TImplementation>()
        where TService : class
        where TImplementation : AbstractRepository, TService
    {
        RepositoryFactories.Add(typeof(TService), ActivatorUtilities.CreateFactory(typeof(TImplementation), [typeof(IDbTransaction)]));
    }

    public void AddScriptsFromAssembly(Assembly assembly)
    {
        DatabaseScriptsAssemblies = DatabaseScriptsAssemblies.Append(assembly);
    }

    public void AddScriptsFromAssemblyWithType<T>()
    {
        AddScriptsFromAssembly(typeof(T).Assembly);
    }

}
