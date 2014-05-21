﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using RequireJsNet.Models;

namespace RequireJsNet.Configuration
{
    using System;
    using System.Web.UI;

    using RequireJsNet.Helpers;

    internal class XmlReader : IConfigReader
    {
        private readonly string path;

        private readonly ConfigLoaderOptions options;

        public XmlReader(string path, ConfigLoaderOptions options)
        {
            this.path = path;
            this.options = options;
        }

        public string Path
        {
            get
            {
                return this.path;
            }
        }

        public ConfigurationCollection ReadConfig()
        {
            using (var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var doc = XDocument.Load(stream);
                var collection = new ConfigurationCollection();
                collection.FilePath = Path;
                collection.Paths = GetPaths(doc.Root);
                collection.Shim = GetShim(doc.Root);
                collection.Map = GetMap(doc.Root);

                if (options.ProcessBundles)
                {
                    collection.Bundles = this.GetBundles(doc.Root);    
                }

                collection.AutoBundles = this.GetAutoBundles(doc.Root);

                return collection;    
            }
        }

        private RequirePaths GetPaths(XElement root)
        {
            var paths = new RequirePaths();
            paths.PathList = new List<RequirePath>();
            var pathEl = root.Descendants("paths").FirstOrDefault();
            if (pathEl != null)
            {
                paths.PathList = pathEl.Descendants("path")
                                        .Select(r => new RequirePath
                                                    {
                                                        Key = r.ReadStringAttribute("key"),
                                                        Value = r.ReadStringAttribute("value"),
                                                        DefaultBundle = r.ReadStringAttribute("bundle", AttributeReadType.Optional)
                                                    }).ToList();
            }

            return paths;
        }

        private RequireShim GetShim(XElement root)
        {
            var shim = new RequireShim();
            shim.ShimEntries = new List<ShimEntry>();
            var shimEl = root.Descendants("shim").FirstOrDefault();
            if (shimEl != null)
            {
                shim.ShimEntries = shimEl.Descendants("dependencies")
                                        .Select(ShimEntryReader)
                                        .ToList();
            }

            return shim;
        }

        private RequireBundles GetBundles(XElement root)
        {
            var bundles = new RequireBundles();
            bundles.BundleEntries = new List<RequireBundle>();
            var bundlesEl = root.Descendants("bundles").FirstOrDefault();
            if (bundlesEl != null)
            {
                bundles.BundleEntries = root.Descendants("bundle").Select(this.BundleEntryReader).ToList();
            }

            return bundles;
        }

        private ShimEntry ShimEntryReader(XElement element)
        {
            return new ShimEntry
            {
                Exports = element.ReadStringAttribute("exports", AttributeReadType.Optional),
                For = element.ReadStringAttribute("for"),
                Dependencies = DependenciesReader(element)
            };
        }

        private List<RequireDependency> DependenciesReader(XElement element)
        {
            return element.Descendants("add")
                        .Select(x => new RequireDependency
                        {
                            Dependency = x.ReadStringAttribute("dependency")
                        }).ToList();
        }

        private RequireBundle BundleEntryReader(XElement element)
        {
            return new RequireBundle
                       {
                           Name = element.ReadStringAttribute("name"),
                           Includes = element.ReadStringListAttribute("includes", AttributeReadType.Optional) ?? new List<string>(),
                           OutputPath = element.ReadStringAttribute("outputPath", AttributeReadType.Optional),
                           IsVirtual = element.ReadBooleanAttribute("virtual", AttributeReadType.Optional) ?? false,
                           BundleItems = element.Descendants("bundleItem")
                                                .Select(r => new BundleItem
                                                                {
                                                                    ModuleName = r.ReadStringAttribute("path"),
                                                                    CompressionType = r.ReadStringAttribute(
                                                                                        "compression", 
                                                                                        AttributeReadType.Optional)
                                                                })
                                                .ToList()
                       };
        }

        private RequireMap GetMap(XElement root)
        {
            var map = new RequireMap();
            map.MapElements = new List<RequireMapElement>();
            var mapEl = root.Descendants("map").FirstOrDefault();
            if (mapEl != null)
            {
                map.MapElements = mapEl.Descendants("replace")
                                        .Select(MapElementReader)
                                        .ToList();
            }

            return map;
        }

        private RequireMapElement MapElementReader(XElement element)
        {
            return new RequireMapElement
                   {
                       For = element.ReadStringAttribute("for"),
                       Replacements = ReplacementsReader(element)
                   };
        }

        private List<RequireReplacement> ReplacementsReader(XElement element)
        {
            return element.Descendants("add")
                            .Select(x => new RequireReplacement
                                         {
                                             NewKey = x.ReadStringAttribute("new"),
                                             OldKey = x.ReadStringAttribute("old")
                                         }).ToList();
        }

        private AutoBundles GetAutoBundles(XElement root)
        {
            var autoBundles = new AutoBundles();
            autoBundles.Bundles = new List<AutoBundle>();
            var autoBundlesEl = root.Descendants("autoBundles").FirstOrDefault();
            if (autoBundlesEl != null)
            {
                autoBundles.Bundles = root.Descendants("autoBundle").Select(this.AutoBundleReader).ToList();
            }

            return autoBundles;
        }

        private AutoBundle AutoBundleReader(XElement element)
        {
            return new AutoBundle
                       {
                           Id = element.ReadStringAttribute("id"),
                           OutputPath = element.ReadStringAttribute("outputPath"),
                           Includes = element.Descendants("include").Select(AutoBundleItemReader).ToList(),
                           Excludes = element.Descendants("exclude").Select(AutoBundleItemReader).ToList()
                       };
        }

        private AutoBundleItem AutoBundleItemReader(XElement element)
        {
            return new AutoBundleItem
                       {
                           BundleId = element.ReadStringAttribute("bundleId", AttributeReadType.Optional),
                           Directory = element.ReadStringAttribute("directory", AttributeReadType.Optional),
                           File = element.ReadStringAttribute("file", AttributeReadType.Optional)
                       };
        }
    }
}
