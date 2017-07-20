﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Quidjibo.Commands;
using Quidjibo.Extensions;
using Quidjibo.Factories;
using Quidjibo.Misc;
using Quidjibo.Models;
using Quidjibo.Providers;
using Quidjibo.Serializers;

namespace Quidjibo.Clients
{
    public class QuidjiboClient : QuidjiboClient<DefaultClientKey>, IQuidjiboClient
    {
        public QuidjiboClient(
            IWorkProviderFactory workProviderFactory,
            IScheduleProviderFactory scheduleProviderFactory,
            IPayloadSerializer payloadSerializer,
            ICronProvider cronProvider) : base(
            workProviderFactory,
            scheduleProviderFactory,
            payloadSerializer,
            cronProvider)
        {
        }
    }

    public class QuidjiboClient<TKey> : IQuidjiboClient<TKey>
        where TKey : IQuidjiboClientKey
    {
        public static IQuidjiboClient<TKey> Instance { get; set; }

        private static readonly ConcurrentDictionary<ProviderCacheKey<TKey>, IWorkProvider> WorkProviders = new ConcurrentDictionary<ProviderCacheKey<TKey>, IWorkProvider>();
        private static readonly ConcurrentDictionary<ProviderCacheKey<TKey>, IScheduleProvider> ScheduleProviders = new ConcurrentDictionary<ProviderCacheKey<TKey>, IScheduleProvider>();

        private readonly ICronProvider _cronProvider;
        private readonly IPayloadSerializer _payloadSerializer;
        private readonly IScheduleProviderFactory _scheduleProviderFactory;

        private readonly IWorkProviderFactory _workProviderFactory;

        public QuidjiboClient(
            IWorkProviderFactory workProviderFactory,
            IScheduleProviderFactory scheduleProviderFactory,
            IPayloadSerializer payloadSerializer,
            ICronProvider cronProvider)
        {
            _workProviderFactory = workProviderFactory;
            _scheduleProviderFactory = scheduleProviderFactory;
            _payloadSerializer = payloadSerializer;
            _cronProvider = cronProvider;
        }

        public async Task PublishAsync(IQuidjiboCommand command,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await PublishAsync(command, 0, cancellationToken);
        }

        public async Task PublishAsync(IQuidjiboCommand command, int delay,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var queueName = command.GetQueueName();
            await PublishAsync(command, queueName, delay, cancellationToken);
        }

        public async Task PublishAsync(IQuidjiboCommand command, string queueName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await PublishAsync(command, queueName, 0, cancellationToken);
        }

        public async Task PublishAsync(IQuidjiboCommand command, string queueName, int delay,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var payload = await _payloadSerializer.SerializeAsync(command, cancellationToken);
            var item = new WorkItem
            {
                Id = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                Name = command.GetName(),
                Attempts = 0,
                Payload = payload,
                Queue = queueName
            };
            var provider = await GetOrCreateWorkProvider(queueName, cancellationToken);
            await provider.SendAsync(item, delay, cancellationToken);
        }

        public async Task ScheduleAsync(string name, IQuidjiboCommand command, Cron cron, CancellationToken cancellationToken = default(CancellationToken))
        {
            var queueName = command.GetQueueName();
            await ScheduleAsync(name, queueName, command, cron, cancellationToken);
        }

        public async Task ScheduleAsync(string name, string queue, IQuidjiboCommand command, Cron cron, CancellationToken cancellationToken = default(CancellationToken))
        {
            var now = DateTime.UtcNow;
            var payload = await _payloadSerializer.SerializeAsync(command, cancellationToken);
            var item = new ScheduleItem
            {
                CreatedOn = now,
                CronExpression = cron.Expression,
                EnqueueOn = _cronProvider.GetNextSchedule(cron.Expression),
                Id = Guid.NewGuid(),
                Name = name,
                Payload = payload,
                Queue = queue,
                VisibleOn = now
            };
            var provider = await GetOrCreateScheduleProvider(queue, cancellationToken);
            var existingItem = await provider.LoadByNameAsync(name, cancellationToken);
            if (existingItem != null)
            {
                if (!item.EquivalentTo(existingItem))
                {
                    await provider.DeleteAsync(existingItem.Id, cancellationToken);
                    await provider.CreateAsync(item, cancellationToken);
                }
            }
            else
            {
                await provider.CreateAsync(item, cancellationToken);
            }
        }

        public void Clear()
        {
            Instance = null;
            WorkProviders.Clear();
            ScheduleProviders.Clear();
        }

        private async Task<IWorkProvider> GetOrCreateWorkProvider(string queueName, CancellationToken cancellationToken)
        {
            IWorkProvider provider;
            var key = new ProviderCacheKey<TKey>(queueName);
            if (!WorkProviders.TryGetValue(key, out provider))
            {
                provider = await _workProviderFactory.CreateAsync(queueName, cancellationToken);
                WorkProviders.TryAdd(key, provider);
            }
            return provider;
        }

        private async Task<IScheduleProvider> GetOrCreateScheduleProvider(string queueName, CancellationToken cancellationToken)
        {
            IScheduleProvider provider;
            var key = new ProviderCacheKey<TKey>(queueName);
            if (!ScheduleProviders.TryGetValue(key, out provider))
            {
                provider = await _scheduleProviderFactory.CreateAsync(queueName, cancellationToken);
                ScheduleProviders.TryAdd(key, provider);
            }
            return provider;
        }
    }
}