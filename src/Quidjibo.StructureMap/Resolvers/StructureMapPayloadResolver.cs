﻿using System;
using Quidjibo.Resolvers;
using StructureMap;

namespace Quidjibo.StructureMap.Resolvers
{
    public class StructureMapPayloadResolver : IPayloadResolver
    {
        private readonly IContainer _container;
        private IContainer _nestedLifetimeScope;

        public StructureMapPayloadResolver(IContainer container)
        {
            _container = container;
        }

        public IDisposable Begin()
        {
            _nestedLifetimeScope = _container.GetNestedContainer();
            return _nestedLifetimeScope;
        }

        public object Resolve(Type type)
        {
            return _nestedLifetimeScope.GetInstance(type);
        }
    }
}