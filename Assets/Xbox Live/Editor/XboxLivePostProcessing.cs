// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
namespace Assets.Xbox_Live.Editor
{
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using Microsoft.Xbox.Services;

    using UnityEditor;
    using UnityEditor.Callbacks;

    using UnityEngine;

    /// <summary>
    /// Handles post processing the generated Visual Studio projects in order to deal with DevCenter
    /// association and including Xbox Live configuration files.
    /// </summary>
    [InitializeOnLoad]
    public class XboxLivePostProcessing
    {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string solutionFolder)
        {
            // When we're building for UWP we need to do some extra stuff.
            if (target == BuildTarget.WSAPlayer)
            {
                XboxLiveConfigurationEditor editorWindow = EditorWindow.GetWindow<XboxLiveConfigurationEditor>("Xbox Live");

                if (editorWindow.configuration == null || editorWindow.configuration.TitleId == 0)
                {
                    Debug.LogWarning("Unable to find a valid Xbox Live configuration file so Xbox Live will not be enabled on UWP project.\r\nOpen \"Xbox Live > Configuration\" and click \"Run Xbox Live Association Wizard\" to configure for Xbox Live access.");
                    return;
                }

                string projectFolder = Path.Combine(solutionFolder, Application.productName);
                UpdateProjectFile(projectFolder, editorWindow.configuration);
            }
        }

        private static void CopyConfigurationFile(string filename, string projectFolder)
        {
            File.Copy(
                Path.Combine(Application.dataPath, "../" + filename),
                Path.Combine(projectFolder, filename),
                true);
        }

        private static void UpdateProjectFile(string projectFolder, XboxLiveAppConfiguration configuration)
        {
            var scriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.WSA);

            // Copy the XboxServices.config file over to the project folder.
            CopyConfigurationFile(XboxLiveAppConfiguration.FileName, projectFolder);

            string projectFile = null;
            if (scriptingBackend == ScriptingImplementation.WinRTDotNET)
            {
                projectFile = Path.Combine(projectFolder, Application.productName + ".csproj");
            }
            else if (scriptingBackend == ScriptingImplementation.IL2CPP)
            {
                projectFile = Path.Combine(projectFolder, Application.productName + ".vcxproj");
            }

            XDocument project = XDocument.Load(projectFile);
            XNamespace msb = "http://schemas.microsoft.com/developer/msbuild/2003";
            XmlNamespaceManager ns = new XmlNamespaceManager(new NameTable());
            ns.AddNamespace("msb", msb.NamespaceName);

            // Add the XboxService.config to the project if it doesn't exist already.
            XElement identityItemGroup = project.XPathSelectElement("msb:Project/msb:ItemGroup[msb:AppxManifest]", ns);

            if (scriptingBackend == ScriptingImplementation.WinRTDotNET)
            {
                if (identityItemGroup.XPathSelectElement("msb:Content[@Include='XboxServices.config']", ns) == null)
                {
                    identityItemGroup.Add(new XElement(msb + "Content",
                        new XAttribute("Include", "XboxServices.config"),
                        new XElement(msb + "CopyToOutputDirectory", "PreserveNewest")));
                }
            }
            else if (scriptingBackend == ScriptingImplementation.IL2CPP)
            {
                if (identityItemGroup.XPathSelectElement("msb:None[@Include='XboxServices.config']", ns) == null)
                {
                    identityItemGroup.Add(new XElement(msb + "None",
                        new XAttribute("Include", "XboxServices.config"),
                        new XElement(msb + "DeploymentContent", "true")));
                }
            }

            project.Save(projectFile);
        }
    }
}