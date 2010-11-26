﻿using System;
using System.Collections.Generic;
using AutoMapper;

namespace Composable.AutoMapper
{
    public class SafeConfiguration
    {
        private IDictionary<Type, ISet<Type>> _mapping = new Dictionary<Type, ISet<Type>>();
        private readonly Configuration _configuration;

        public SafeConfiguration(Configuration configuration)
        {
            this._configuration = configuration;
        }

        public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            Type targetType = typeof(TDestination);
            Type sourceType = typeof(TSource);

            if (!_mapping.ContainsKey(sourceType))
            {
                _mapping[sourceType] = new HashSet<Type>();
            }
            
            if(_mapping[sourceType].Contains(targetType))
            {
                throw new DuplicateMappingException(sourceType, targetType);
            }

            _mapping[sourceType].Add(targetType);
            return _configuration.CreateMap<TSource, TDestination>();

        }
    }
}