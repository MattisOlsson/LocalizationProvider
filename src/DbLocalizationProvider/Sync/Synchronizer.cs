// Copyright (c) Valdis Iljuconoks. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DbLocalizationProvider.Abstractions;
using DbLocalizationProvider.Cache;
using DbLocalizationProvider.Commands;
using DbLocalizationProvider.Internal;
using DbLocalizationProvider.Queries;

namespace DbLocalizationProvider.Sync
{
    /// <summary>
    /// This class is responsible for trigger underlying storage schema sync process at correct time.
    /// </summary>
    public class Synchronizer : ISynchronizer
    {
        private readonly TypeDiscoveryHelper _helper;
        private static readonly ThreadSafeSingleShotFlag _synced = false;

        /// <summary>
        /// Initializes new instance of the resource scanner.
        /// </summary>
        /// <param name="helper">Discovery helper to use to locate resources.</param>
        public Synchronizer(TypeDiscoveryHelper helper)
        {
            _helper = helper;
        }

        /// <summary>
        /// Registers manually crafted resources.
        /// </summary>
        /// <param name="resources">List of resources.</param>
        public void RegisterManually(IEnumerable<ManualResource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            foreach (var manualResource in resources)
            {
                var existingResource = new GetResource.Query(manualResource.Key).Execute();
                if (existingResource == null)
                {
                    var resourceToSync = new LocalizationResource(manualResource.Key)
                    {
                        Author = "manual",
                        FromCode = false,
                        IsModified = false,
                        IsHidden = false,
                        ModificationDate = DateTime.UtcNow
                    };

                    resourceToSync.Translations.Add(new LocalizationResourceTranslation
                    {
                        Language = manualResource.Language.Name,
                        Value = manualResource.Translation
                    });

                    var c = new CreateNewResources.Command(new List<LocalizationResource> { resourceToSync });
                    c.Execute();
                }
                else
                {
                    var c = new CreateOrUpdateTranslation.Command(
                        manualResource.Key,
                        manualResource.Language,
                        manualResource.Translation);

                    c.Execute();
                }
            }
        }

        /// <summary>
        /// Updates the underlying storage schema.
        /// </summary>
        public void UpdateStorageSchema()
        {
            var command = new UpdateSchema.Command();
            if (!command.CanBeExecuted())
            {
                throw new InvalidOperationException(
                    "Resource sync handler is not registered. Make sure that storage provider is registered e.g. ctx.UseSqlServer(..)");
            }

            if (!_synced)
            {
                command.Execute();
            }
        }

        /// <summary>
        /// Synchronizes resources.
        /// </summary>
        /// <param name="registerResources">If <c>true</c> discovered resources are stored in underlying database</param>
        public void SyncResources(bool registerResources)
        {
            var resources = registerResources ? DiscoverReadMerge() : ReadMerge();

            StoreKnownResourcesAndPopulateCache(resources);
        }

        private IEnumerable<LocalizationResource> ReadMerge()
        {
            return new GetAllResources.Query(true).Execute();
        }

        private IEnumerable<LocalizationResource> DiscoverReadMerge()
        {
            UpdateStorageSchema();

            var discoveredTypes = _helper.GetTypes(
                t => t.GetCustomAttribute<LocalizedResourceAttribute>() != null,
                t => t.GetCustomAttribute<LocalizedModelAttribute>() != null);

            var discoveredResourceTypes = discoveredTypes[0];
            var discoveredModelTypes = discoveredTypes[1];
            var foreignResourceTypes = ConfigurationContext.Current.ForeignResources;

            if (foreignResourceTypes != null && foreignResourceTypes.Any())
            {
                discoveredResourceTypes.AddRange(foreignResourceTypes.Select(x => x.ResourceType));
            }

            ICollection<DiscoveredResource> discoveredResources = new List<DiscoveredResource>();
            ICollection<DiscoveredResource> discoveredModels = new List<DiscoveredResource>();

            Parallel.Invoke(() => discoveredResources = DiscoverResources(discoveredResourceTypes),
                            () => discoveredModels = DiscoverResources(discoveredModelTypes));

            var syncCommand = new SyncResources.Query(discoveredResources, discoveredModels);
            var syncedResources = syncCommand.Execute();

            return syncedResources;
        }

        private ICollection<DiscoveredResource> DiscoverResources(List<Type> types)
        {
            var properties = types.SelectMany(type => _helper.ScanResources(type)).DistinctBy(r => r.Key).ToList();

            return properties;
        }

        private static void StoreKnownResourcesAndPopulateCache(IEnumerable<LocalizationResource> syncedResources)
        {
            if (ConfigurationContext.Current.PopulateCacheOnStartup)
            {
                new ClearCache.Command().Execute();

                foreach (var resource in syncedResources)
                {
                    var key = CacheKeyHelper.BuildKey(resource.ResourceKey);
                    ConfigurationContext.Current.CacheManager.Insert(key, resource, true);
                }
            }
            else
            {
                // just store resource cache keys
                syncedResources.ForEach(r => ConfigurationContext.Current.BaseCacheManager.StoreKnownKey(r.ResourceKey));
            }
        }
    }
}
