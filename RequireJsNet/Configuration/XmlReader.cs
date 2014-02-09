﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

using RequireJsNet.Models;

namespace RequireJsNet.Configuration
{
    internal class XmlReader : IConfigReader
    {
        private readonly string _path;
        public string Path { get { return _path; } }

        public XmlReader(string path)
        {
            _path = path;
        }

        public ConfigurationCollection ReadConfig()
        {
            using (var stream = new FileStream(Path, FileMode.Open))
            {
                var doc = XDocument.Load(stream);
                var collection = new ConfigurationCollection();
                collection.FilePath = Path;
                collection.Paths = GetPaths(doc.Root);
                collection.Shim = GetShim(doc.Root);
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
                paths.PathList = pathEl.Descendants("path").Select(r => new RequirePath
                                                                        {
                                                                            Key = r.Attribute("key").Value,
                                                                            Value = r.Attribute("value").Value
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
                                        .Select(r => new ShimEntry
                                                    {
                                                        Exports = r.Attribute("exports").Value,
                                                        For = r.Attribute("for").Value,
                                                        Dependencies = r.Descendants("add")
                                                                            .Select(x => new RequireDependency
                                                                                        {
                                                                                            Dependency = x.Attribute("dependency").Value
                                                                                        }).ToList()
                                                    }).ToList();
            }
            return shim;
        }

    }
}