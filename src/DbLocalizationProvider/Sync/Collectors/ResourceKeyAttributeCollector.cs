﻿// Copyright (c) 2018 Valdis Iljuconoks.
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DbLocalizationProvider.Abstractions;
using DbLocalizationProvider.Internal;

namespace DbLocalizationProvider.Sync.Collectors
{
    internal class ResourceKeyAttributeCollector : IResourceCollector
    {
        public IEnumerable<DiscoveredResource> GetDiscoveredResources(Type target,
            object instance,
            MemberInfo mi,
            string translation,
            string resourceKey,
            string resourceKeyPrefix,
            bool typeKeyPrefixSpecified,
            bool isHidden,
            string typeOldName,
            string typeOldNamespace,
            Type declaringType,
            Type returnType,
            bool isSimpleType)
        {
            // check if there are [ResourceKey] attributes
            var keyAttributes = mi.GetCustomAttributes<ResourceKeyAttribute>().ToList();

            return keyAttributes.Select(attr =>
                                        {
                                            var translations = DiscoveredTranslation.FromSingle(string.IsNullOrEmpty(attr.Value) ? translation : attr.Value);

                                            var additionalTranslations = mi.GetCustomAttributes<TranslationForCultureAttribute>();
                                            if(additionalTranslations != null && additionalTranslations.Any())
                                            {
                                                if(additionalTranslations.GroupBy(t => t.Culture).Any(g => g.Count() > 1))
                                                    throw new DuplicateResourceTranslationsException($"Duplicate translations for the same culture for following resource: `{resourceKey}`");

                                                additionalTranslations.ForEach(t =>
                                                                               {
                                                                                   var existingTranslation = translations.FirstOrDefault(_ => _.Culture == t.Culture);
                                                                                   if(existingTranslation != null)
                                                                                       existingTranslation.Translation = t.Translation;
                                                                                   else
                                                                                       translations.Add(new DiscoveredTranslation(t.Translation, t.Culture));
                                                                               });
                                            }

                                            return new DiscoveredResource(mi,
                                                    ResourceKeyBuilder.BuildResourceKey(typeKeyPrefixSpecified ? resourceKeyPrefix : null, attr.Key, string.Empty),
                                                    translations,
                                                    null,
                                                    declaringType,
                                                    returnType,
                                                    true)
                                            {
                                                FromResourceKeyAttribute = true
                                            };
                                        });
        }
    }
}
