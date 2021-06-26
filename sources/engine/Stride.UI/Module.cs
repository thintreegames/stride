// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Reflection;
using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.UI
{
    class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);

            if (!Directory.Exists("resources"))
            {
                Directory.CreateDirectory("resources");
            }

            File.WriteAllBytes("resources/cacert.pem", Ultralight.cacert);
            File.WriteAllBytes("resources/icudt67l.dat", Ultralight.icudt67l);
        }
    }
}
