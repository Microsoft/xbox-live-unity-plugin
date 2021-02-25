// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
namespace Assets.Xbox_Live.Editor
{
    using System;
    using Microsoft.Xbox.Services;
    using UnityEditor;

    /// <summary>
    /// Handles post processing the generated Visual Studio projects in order to deal with DevCenter
    /// association and including Xbox Live configuration files.
    /// </summary>
    public class XboxLivePostProcessingUnityProject : AssetPostprocessor
    {
        private static string OnGeneratedCSProject(string path, string content)
        {
            return AddXboxServicesConfig(path, content);
        }

        /// <summary>
        /// Adds the XboxServices.config file to the generated project file.
        /// </summary>
        /// <param name="fileName">The name of the file being processed.</param>
        /// <param name="fileContent">The content of the file being processed.</param>
        /// <returns>The project file with an additional content element included.</returns>
        /// <remarks>
        /// This only modifies the Unity debug projects and will not have any
        /// effect on projects built as part of the Unity UWP build process.
        /// </remarks>
        private static string AddXboxServicesConfig(string fileName, string fileContent)
        {
            if (!fileName.EndsWith(".Editor.csproj"))
            {
                const string configFileElement =
                    "    <Content Include=\"" + XboxLiveAppConfiguration.FileName + "\">\r\n" +
                    "       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>\r\n" +
                    "    </Content>\r\n";

                // Hacky way to do this for now.  Should make it a bit more stable.
                int lastItemGroup = fileContent.LastIndexOf("  </ItemGroup>", StringComparison.OrdinalIgnoreCase);
                fileContent = fileContent.Insert(lastItemGroup, configFileElement);
            }

            return fileContent;
        }
    }
}
