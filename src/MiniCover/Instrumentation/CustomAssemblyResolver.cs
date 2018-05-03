﻿using Microsoft.Extensions.DependencyModel;
using MiniCover.Utils;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiniCover.Instrumentation
{
    public class CustomAssemblyResolver : DefaultAssemblyResolver
    {
        private DependencyContext _dependencyContext;

        public CustomAssemblyResolver(string assemblyDirectory)
        {
            RemoveSearchDirectory(".");
            RemoveSearchDirectory("bin");

            AddSearchDirectory(assemblyDirectory);

            _dependencyContext = DepsJsonUtils.LoadDependencyContext(assemblyDirectory);

            var runtimeConfigPath = Directory.GetFiles(assemblyDirectory, "*.runtimeconfig.dev.json", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (runtimeConfigPath != null)
            {
                var runtimeConfigContent = File.ReadAllText(runtimeConfigPath);
                foreach (var path in DepsJsonUtils.GetAdditionalPaths(runtimeConfigContent))
                {
                    AddSearchDirectory(path);
                }
            }
        }

        private AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
        {
            if (parameters.AssemblyResolver == null)
            {
                parameters.AssemblyResolver = this;
            }

            return ModuleDefinition.ReadModule(file, parameters).Assembly;
        }

        protected override AssemblyDefinition SearchDirectory(AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
        {
            if (_dependencyContext != null)
            {
                var dependency = _dependencyContext.CompileLibraries.FirstOrDefault(c =>
                {
                    return c.Name == name.Name;
                });

                if (dependency != null)
                {
                    foreach (var depAssembly in dependency.Assemblies)
                    {
                        foreach (var directory in directories)
                        {
                            var file = Path.Combine(new[] { directory, dependency.Path, depAssembly }.Where(x => x != null).ToArray());
                            if (File.Exists(file))
                            {
                                try
                                {
                                    return GetAssembly(file, parameters);
                                }
                                catch (BadImageFormatException)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"DependencyContext.CompileLibraries doesn't contain information about assebmly {name.Name}!");
                    _dependencyContext.CompileLibraries.ToList().ForEach(lib => Console.WriteLine(lib.Name));
                }
            }
            else
            {
                Console.WriteLine("Dependency context is null!");
            }

            return base.SearchDirectory(name, directories, parameters);
        }
    }
}
