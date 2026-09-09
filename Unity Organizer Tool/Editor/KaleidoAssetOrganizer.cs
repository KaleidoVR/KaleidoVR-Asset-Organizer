// =========================================================================
// BLOCK 1: GLOBAL ENGINE DIRECTIVES & ROOT MODULE CONFIGURATION
// KaleidoVR Asset Organizer
// Created by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. Released under the MIT License.
// Compatible with Unity 2022.3.22f1 through Unity 6 (6000.x)
// VRChat SDK3 Avatars optional (Auto-Link FX & Menu)
// Uses 2022.3 LTS AssetDatabase/PrefabUtility APIs only (no 2023+/Unity 6-only types)
// =========================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace KaleidoVR.EditorTools
{
    public class KaleidoAssetOrganizer : EditorWindow
    {
        public static readonly string VERSION = "1.0.8";
        public const string LOGO_FILE_NAME = "Kali_Logo.png";
        public const string FALLBACK_ICON_PATH = "Assets/KaleidoVR/Editor/Icons/Kali_Logo.png";

        // Resolved from this script's own location, so the tool folder can be renamed or moved.
        public static string ICON_PATH { get { return ResolveIconPath(); } }

        private static string cachedIconPath;

        private Texture2D headerIcon;

        public string outputDirectory = "Assets/KaleidoVR/Models/Test";
        public string sceneName = "OrganizedScene";
        public string prefabName = "NewAvatar";
        public bool createPrefab = true;
        // =========================================================================
        // BLOCK 2: AUTOMATION PIPELINES VARIABLE DECLARATIONS
        // =========================================================================

        public bool autoParsePoiyomi = true;
        public bool autoSetupVRCDescriptor = true;

        public List<UnityEngine.Object> objectsToOrganize = new List<UnityEngine.Object>();
        public List<UnityEngine.Object> ignoreList = new List<UnityEngine.Object>();
        // =========================================================================
        // BLOCK 3: SORTER PIPELINE LOOKUP ROUTING DICTIONARY
        // =========================================================================

        public Dictionary<string, string> organizeOptions = new Dictionary<string, string>()
        {
            {"GameObject", "Copy"},
            {"Texture2D", "Copy"},
            {"Cubemap", "Copy"},
            {"Material", "Copy"},
            {"VRCExpressionParameters", "Copy"},
            {"VRCExpressionsMenu", "Copy"},
            {"BlendTree", "Copy"},
            {"AnimationClip", "Copy"},
            {"AnimatorOverrideController", "Copy"},
            {"AnimatorController", "Copy"},        // FIXED: Explicitly mapped for native Unity .controller files
            {"RuntimeAnimatorController", "Copy"}, // FIXED: Explicitly mapped for runtime controller asset references
            {"AvatarMask", "Copy"},
            {"AudioClip", "Copy"},
            {"Shader", "Ignore"},
            {"MonoScript", "Ignore"},
            {"DefaultAsset", "Ignore"}
        };
        // =========================================================================
        // BLOCK 4: MENU ITEMS & TEXTURE CACHE LOADING WITH EDITORPREFS RESTORERS
        // =========================================================================

        [MenuItem("KaleidoVR/Asset Organizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<KaleidoAssetOrganizer>("Asset Organizer");
            window.InitializeLocalLogo();
            window.LoadEditorPreferences(); // Load saved properties on window instantiation
            window.ResizeWindow();
        }

        private void OnEnable()
        {
            InitializeLocalLogo();
            LoadEditorPreferences(); // Fallback reload pass when the assembly compilation changes
        }

        private void InitializeLocalLogo()
        {
            cachedIconPath = null;
            headerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
        }

        // Locates this script in the project so Icons/ is found relative to it instead of a fixed folder name
        public static string GetToolEditorFolder()
        {
            foreach (string guid in AssetDatabase.FindAssets("KaleidoAssetOrganizer t:MonoScript"))
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (scriptPath.EndsWith("/KaleidoAssetOrganizer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    return scriptPath.Substring(0, scriptPath.LastIndexOf('/'));
                }
            }
            return null;
        }

        private static string ResolveIconPath()
        {
            if (!string.IsNullOrEmpty(cachedIconPath)) return cachedIconPath;

            string editorFolder = GetToolEditorFolder();
            if (!string.IsNullOrEmpty(editorFolder))
            {
                string relativeIcon = editorFolder + "/Icons/" + LOGO_FILE_NAME;
                if (AssetDatabase.LoadMainAssetAtPath(relativeIcon) != null)
                {
                    cachedIconPath = relativeIcon;
                    return cachedIconPath;
                }
            }

            // Last resort: the logo may have been placed anywhere in the project
            string logoName = Path.GetFileNameWithoutExtension(LOGO_FILE_NAME);
            foreach (string guid in AssetDatabase.FindAssets(logoName + " t:Texture2D"))
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (foundPath.EndsWith("/" + LOGO_FILE_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    cachedIconPath = foundPath;
                    return cachedIconPath;
                }
            }

            cachedIconPath = !string.IsNullOrEmpty(editorFolder) ? editorFolder + "/Icons/" + LOGO_FILE_NAME : FALLBACK_ICON_PATH;
            return cachedIconPath;
        }

        // Internal engine caching tracker pass to recover data keys from your hard disk environment
        private void LoadEditorPreferences()
        {
            if (EditorPrefs.HasKey("KVR_OutputDir"))
            {
                outputDirectory = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(EditorPrefs.GetString("KVR_OutputDir"));
            }
            if (EditorPrefs.HasKey("KVR_SceneName")) sceneName = EditorPrefs.GetString("KVR_SceneName");
            if (EditorPrefs.HasKey("KVR_PrefabName")) prefabName = EditorPrefs.GetString("KVR_PrefabName");
            if (EditorPrefs.HasKey("KVR_CreatePrefab")) createPrefab = EditorPrefs.GetBool("KVR_CreatePrefab");
            if (EditorPrefs.HasKey("KVR_AutoParsePoi")) autoParsePoiyomi = EditorPrefs.GetBool("KVR_AutoParsePoi");
            if (EditorPrefs.HasKey("KVR_AutoSetupVRC")) autoSetupVRCDescriptor = EditorPrefs.GetBool("KVR_AutoSetupVRC");

            // Loop through options keys matrix to load each specific Export dropdown action choice state
            List<string> keys = new List<string>(organizeOptions.Keys);
            foreach (string key in keys)
            {
                string prefKey = "KVR_Opt_" + key;
                if (EditorPrefs.HasKey(prefKey))
                {
                    organizeOptions[key] = EditorPrefs.GetString(prefKey);
                }
            }
        }

        // Saves selection states down to preferences instantly when values change
        public void SaveEditorPreferences()
        {
            EditorPrefs.SetString("KVR_OutputDir", outputDirectory);
            EditorPrefs.SetString("KVR_SceneName", sceneName);
            EditorPrefs.SetString("KVR_PrefabName", prefabName);
            EditorPrefs.SetBool("KVR_CreatePrefab", createPrefab);
            EditorPrefs.SetBool("KVR_AutoParsePoi", autoParsePoiyomi);
            EditorPrefs.SetBool("KVR_AutoSetupVRC", autoSetupVRCDescriptor);

            foreach (KeyValuePair<string, string> kvp in organizeOptions)
            {
                EditorPrefs.SetString("KVR_Opt_" + kvp.Key, kvp.Value);
            }
        }
        // =========================================================================
        // BLOCK 5: WINDOW GUI RENDER ENTRYPOINT LOOPS & SERIALIZATION HOOKS
        // =========================================================================

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck(); // Watch the UI canvas frame window layout inputs for adjustments

            KaleidoAssetOrganizerUI.DrawHeader(this, headerIcon, VERSION);
            KaleidoAssetOrganizerUI.DrawOutputDirectory(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawSettings(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawObjectsToOrganize(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawOrganizeOptions(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawIgnoreList(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawOrganizeButton(this);
            KaleidoAssetOrganizerUI.DrawDiscordButton();
            KaleidoAssetOrganizerUI.DrawFooter(this);

            // If you change a dropdown toggle or directory text box choice field path, save it immediately
            if (EditorGUI.EndChangeCheck())
            {
                SaveEditorPreferences();
            }

            ResizeWindow();
        }
        // =========================================================================
        // BLOCK 6: PANEL BOUNDARY DIMENSION EQUATIONS
        // =========================================================================

        public void ResizeWindow()
        {
            float logoHeight = headerIcon != null ? 200f : 15f;
            float outputDirHeight = 45f;
            float settingsHeight = 115f;
            float objectsHeight = 65f + (objectsToOrganize.Count * 22f);
            float organizeOptionsHeight = (organizeOptions.Count * 22f) + 70f;
            float ignoreListHeight = 65f + (ignoreList.Count * 22f);
            float organizeButtonHeight = 55f;
            float discordButtonHeight = 28f;
            float footerHeight = 25f;

            float totalHeight = logoHeight + outputDirHeight + settingsHeight + objectsHeight + organizeOptionsHeight + ignoreListHeight + organizeButtonHeight + discordButtonHeight + footerHeight;

            Vector2 targetSize = new Vector2(500, totalHeight);
            minSize = targetSize;
            maxSize = targetSize;
        }
    }
    // =========================================================================
    // BLOCK 7: HELPER MULTI-DROP STACKING RECEIVERS
    // =========================================================================

    public static class KaleidoAssetOrganizerHelpers
    {
        public static void HandleDragAndDrop(Rect dropArea, List<UnityEngine.Object> targetList)
        {
            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj == null) continue;
                        if (!targetList.Contains(obj))
                        {
                            targetList.Add(obj);
                        }
                    }
                    GUI.changed = true;
                }
                Event.current.Use();
            }
        }
        // =========================================================================
        // BLOCK 8: RECURSIVE EXCLUSIONS DEPENDENCY BUILDERS
        // =========================================================================

        public static HashSet<string> BuildRecursiveIgnoreMap(List<UnityEngine.Object> rawIgnoreList)
        {
            HashSet<string> subAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<UnityEngine.Object> collectedObjects = new List<UnityEngine.Object>();

            foreach (var root in rawIgnoreList)
            {
                if (root == null) continue;
                string rootPath = AssetDatabase.GetAssetPath(root);
                if (!string.IsNullOrEmpty(rootPath) && !subAssetPaths.Contains(rootPath))
                {
                    subAssetPaths.Add(rootPath);
                }

                if (root is GameObject go)
                {
                    collectedObjects.Add(go);
                    foreach (Transform child in go.transform) CollectChildObjects(child, collectedObjects, new List<UnityEngine.Object>());
                }
            }
            // =========================================================================
            // BLOCK 9: DOWNSTREAM DEPENDENCIES RESOLVER BLOCKS
            // =========================================================================

            if (collectedObjects.Count > 0)
            {
                var deps = EditorUtility.CollectDependencies(collectedObjects.ToArray());
                foreach (var dep in deps)
                {
                    if (dep == null) continue;
                    string depPath = AssetDatabase.GetAssetPath(dep);
                    if (!string.IsNullOrEmpty(depPath) && !subAssetPaths.Contains(depPath)) subAssetPaths.Add(depPath);
                }
            }
            return subAssetPaths;
        }
        // =========================================================================
        // BLOCK 10: HIERARCHY SCANNERS & SUFFIX FILTER RULES
        // =========================================================================

        public static List<UnityEngine.Object> GetAllObjectsIncludingChildren(List<UnityEngine.Object> roots, List<UnityEngine.Object> ignoreList)
        {
            List<UnityEngine.Object> allObjects = new List<UnityEngine.Object>();
            foreach (var root in roots)
            {
                if (root == null || ignoreList.Contains(root)) continue;
                if (root is GameObject go)
                {
                    allObjects.Add(go);
                    foreach (Transform child in go.transform) CollectChildObjects(child, allObjects, ignoreList);
                }
                else allObjects.Add(root);
            }
            return allObjects;
        }

        private static void CollectChildObjects(Transform parent, List<UnityEngine.Object> list, List<UnityEngine.Object> ignoreList)
        {
            if (parent == null || ignoreList.Contains(parent.gameObject)) return;
            list.Add(parent.gameObject);
            foreach (Transform child in parent) CollectChildObjects(child, list, ignoreList);
        }
        // =========================================================================
        // BLOCK 11: FILE PATH SORTER DESTINATION PARSERS
        // =========================================================================

        public static bool ShouldIgnoreAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            string lower = path.ToLowerInvariant();
            return lower.EndsWith(".cs") || lower.EndsWith(".dll") || lower.EndsWith(".asmdef") || lower.EndsWith(".pdb")
                   || lower.EndsWith(".meta") || lower.StartsWith("packages/")
                   || lower == "assets/editor" || lower.StartsWith("assets/editor/")
                   || lower.StartsWith("library/") || lower.StartsWith("resources/unity_builtin_extra")
                   || lower.Contains("unity default resources") || lower.Contains("unity_builtin_extra");
        }

        public static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return path.Replace("\\", "/").Trim().TrimEnd('/');
        }

        public static bool IsInsideAssets(string path)
        {
            path = NormalizeAssetPath(path);
            return path.Equals("Assets", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSameOrInside(string path, string folder)
        {
            path = NormalizeAssetPath(path);
            folder = NormalizeAssetPath(folder);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(folder)) return false;
            return path.Equals(folder, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryNormalizeOutputDirectory(string raw, out string output, out string error)
        {
            output = NormalizeAssetPath(raw);
            error = null;
            if (string.IsNullOrEmpty(output) || !IsInsideAssets(output))
            {
                error = "Output folder must be inside this project's Assets folder.";
                return false;
            }
            if (output.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                error = "Pick a folder under Assets, not the Assets root. Organizing into Assets would scatter FBX, Materials, and 3.0 folders across the project.";
                return false;
            }
            if (output.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                error = "Output folder cannot be inside Packages.";
                return false;
            }
            return true;
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }
            return name.Trim();
        }

        public static string ResolveExportTypeName(string path, UnityEngine.Object mainAsset, UnityEngine.Object fallback)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".blend", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".vrm", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    return "GameObject";
                }
                if (path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase)) return "AnimatorController";
                if (path.EndsWith(".overrideController", StringComparison.OrdinalIgnoreCase)) return "AnimatorOverrideController";
                if (path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)) return "AnimationClip";
                if (path.EndsWith(".mask", StringComparison.OrdinalIgnoreCase)) return "AvatarMask";
                if (path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) return "Material";
            }

            string typeName = mainAsset != null ? mainAsset.GetType().Name : (fallback != null ? fallback.GetType().Name : string.Empty);
            if ((typeName == "MonoBehaviour" || typeName == "DefaultAsset") && !string.IsNullOrEmpty(path))
            {
                string lowerName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (lowerName.Contains("expression") && (lowerName.Contains("param") || lowerName.Contains("parameter")))
                {
                    return "VRCExpressionParameters";
                }
                if (lowerName.Contains("menu"))
                {
                    return "VRCExpressionsMenu";
                }
            }
            return typeName;
        }

        public static string AliasOrganizeType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return typeName;
            return typeName switch
            {
                "Sprite" => "Texture2D",
                "Texture2DArray" => "Texture2D",
                "Texture3D" => "Texture2D",
                "RenderTexture" => "Texture2D",
                "MovieTexture" => "Texture2D",
                "RuntimeAnimatorController" => "AnimatorController",
                _ => typeName
            };
        }

        public static string InferTransferAction(Dictionary<string, string> options)
        {
            if (options == null) return "Ignore";
            int copy = 0, move = 0;
            foreach (KeyValuePair<string, string> kvp in options)
            {
                if (kvp.Key == "Shader" || kvp.Key == "MonoScript" || kvp.Key == "DefaultAsset") continue;
                if (kvp.Value == "Move") move++;
                else if (kvp.Value == "Copy") copy++;
            }
            if (move > 0 && copy == 0) return "Move";
            if (copy > 0 && move == 0) return "Copy";
            return "Ignore";
        }

        public static string ResolveOrganizeAction(Dictionary<string, string> options, string typeName)
        {
            if (options == null) return "Ignore";
            string key = AliasOrganizeType(typeName);
            if (!string.IsNullOrEmpty(key) && options.ContainsKey(key)) return options[key];
            if (!string.IsNullOrEmpty(typeName) && options.ContainsKey(typeName)) return options[typeName];
            return InferTransferAction(options);
        }

        public static string GetTargetFolder(string typeName, string assetPath = "", bool parsePoiyomi = false)
        {
            if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return "Prefabs";
            }

            if (parsePoiyomi && typeName == "Texture2D" && !string.IsNullOrEmpty(assetPath))
            {
                string lowerName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
                if (lowerName.Contains("normal") || lowerName.EndsWith("_n") || lowerName.Contains("nrm")) return "Textures/Normals";
                if (lowerName.Contains("emission") || lowerName.Contains("emissive") || lowerName.EndsWith("_e")) return "Textures/Emissions";
                if (lowerName.Contains("metallic") || lowerName.Contains("metal") || lowerName.EndsWith("_m")) return "Textures/Metallic";
                if (lowerName.Contains("roughness") || lowerName.Contains("rough") || lowerName.EndsWith("_r")) return "Textures/Roughness";
                if (lowerName.Contains("ao") || lowerName.Contains("occlusion")) return "Textures/AO";
            }

            // FIXED: Standardized type routing names so Animators route consistently into '3.0/Controllers' or 'Other' as intended
            return typeName switch
            {
                "GameObject" => "FBX",
                "Material" => "Materials",
                "Texture2D" => "Textures",
                "Cubemap" => "Textures",
                "AudioClip" => "Audio",
                "AnimationClip" => "3.0/Animations",
                "BlendTree" => "3.0/BlendTrees",
                "AvatarMask" => "3.0/Avatar Masks",
                "AnimatorController" => "3.0/Controllers",
                "RuntimeAnimatorController" => "3.0/Controllers",
                "AnimatorOverrideController" => "3.0/Controllers",
                "VRCExpressionParameters" => "3.0/VRCExpressionParameters",
                "VRCExpressionsMenu" => "3.0/Menus",
                _ => "Other"
            };
        }
    }
    // =========================================================================
    // BLOCK 12: ORGANIZATION ENGINE ENTRYPOINTS & LOGGING SETUP
    // =========================================================================

    public static class KaleidoAssetOrganizerLogic
    {
        public static void OrganizeAssets(KaleidoAssetOrganizer window)
        {
            string projectRoot = GetProjectRootPath();
            string logDir = Path.Combine(projectRoot, "Logs", "KaleidoVR", "Organizer");
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_organizer_log.txt");

            List<string> logEntries = new List<string>() { "KaleidoVR Asset Organization Pipeline Began at " + DateTime.Now };
            Dictionary<string, string> movedAssetsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> copiedAssetsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> copiedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<int> protectedInstanceIds = BuildProtectedInstanceIds(window.objectsToOrganize);
            HashSet<string> protectedAssetPaths = BuildProtectedAssetPaths(window.objectsToOrganize);

            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorUtility.DisplayDialog("KaleidoVR Asset Organizer", "Exit Play Mode before organizing assets.", "OK");
                    return;
                }

                if (!KaleidoAssetOrganizerHelpers.TryNormalizeOutputDirectory(window.outputDirectory, out string outputDirectory, out string outputError))
                {
                    EditorUtility.DisplayDialog("KaleidoVR Asset Organizer", outputError, "OK");
                    return;
                }
                window.outputDirectory = outputDirectory;
                window.SaveEditorPreferences();

                if (IsSelectionAlreadyInOutput(window.objectsToOrganize, outputDirectory))
                {
                    EditorUtility.DisplayDialog(
                        "KaleidoVR Asset Organizer",
                        "The selected object already lives in the output folder.\n\nDrop the original avatar FBX or prefab from its source folder (for example MisterPink/Velle), pick a new empty output folder, and Organize again.\n\nOrganizing the result back into itself will not move leftover files out of the original pack.",
                        "OK");
                    return;
                }

                Debug.Log("[KaleidoVR] Running integrated avatar asset organization tool...");

                EditorUtility.DisplayProgressBar("KaleidoVR Asset Organizer", "Preparing output folders...", 0.02f);

                HashSet<string> fullyIgnoredAssetPaths = KaleidoAssetOrganizerHelpers.BuildRecursiveIgnoreMap(window.ignoreList);

                if (!PrepareOutputFolders(outputDirectory, window.autoParsePoiyomi, logEntries))
                {
                    EditorUtility.DisplayDialog("KaleidoVR Asset Organizer", "Could not create the output folder tree inside Assets. Check the Console and pick another folder.", "OK");
                    return;
                }

                int copied = 0, moved = 0, ignored = 0;
                HashSet<string> projectAssetPaths = CollectProjectAssetPaths(window.objectsToOrganize, window.ignoreList, logEntries);
                int beforePackSweep = projectAssetPaths.Count;
                CollectMovePackAssets(window, projectAssetPaths, logEntries);
                if (projectAssetPaths.Count > beforePackSweep)
                {
                    logEntries.Add("Move pack sweep added " + (projectAssetPaths.Count - beforePackSweep) + " extra files from the avatar folder.");
                }

                if (projectAssetPaths.Count == 0)
                {
                    logEntries.Add("No project assets were found on the selected objects.");
                    EditorUtility.DisplayDialog(
                        "KaleidoVR Asset Organizer",
                        "The selected Hierarchy object did not resolve to any project assets.\n\nDrop the avatar's FBX or prefab from the Project window, or a scene instance that still has a prefab/model source.",
                        "OK");
                    return;
                }

                List<string> dependencies = new List<string>(projectAssetPaths);
                dependencies.Sort(CompareTransferOrder);

                int totalAssets = Mathf.Max(1, dependencies.Count);
                int currentAssetIndex = 0;
                HashSet<string> processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                // =========================================================================
                // BLOCK 13: FILE TRANSFER ITERATOR EXECUTION FRAMEWORK
                // =========================================================================
                // CopyAsset/MoveAsset run with a live Asset Database so folder creation and
                // unique-path checks work the same on 2022.3.22f1 and Unity 6.
                // (AssetDatabase.AssetEditingScope is 2023.1+ only — do not use it here.)

                foreach (string path in dependencies)
                {
                    currentAssetIndex++;
                    if (string.IsNullOrEmpty(path)) continue;

                    float progressPercentage = (float)currentAssetIndex / totalAssets;
                    EditorUtility.DisplayProgressBar(
                        "KaleidoVR Asset Organizer",
                        $"Batch Transferring Assets Safely ({currentAssetIndex}/{totalAssets}): {Path.GetFileName(path)}",
                        progressPercentage
                    );

                    if (processedPaths.Contains(path)) continue;
                    processedPaths.Add(path);

                    if (AssetDatabase.IsValidFolder(path)) continue;

                    if (KaleidoAssetOrganizerHelpers.ShouldIgnoreAsset(path) || fullyIgnoredAssetPaths.Contains(path))
                    {
                        ignored++;
                        logEntries.Add("Ignored: " + path);
                        continue;
                    }

                    if (KaleidoAssetOrganizerHelpers.IsSameOrInside(path, window.outputDirectory))
                    {
                        movedAssetsMap[path] = path;
                        logEntries.Add("Already in output: " + path);
                        continue;
                    }

                    UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                    string typeName = KaleidoAssetOrganizerHelpers.ResolveExportTypeName(path, mainAsset, mainAsset);

                    string action = KaleidoAssetOrganizerHelpers.ResolveOrganizeAction(window.organizeOptions, typeName);
                    if (action == "Ignore")
                    {
                        ignored++;
                        logEntries.Add("Export Ignore: " + path);
                        continue;
                    }

                    string targetFolder = KaleidoAssetOrganizerHelpers.GetTargetFolder(KaleidoAssetOrganizerHelpers.AliasOrganizeType(typeName), path, window.autoParsePoiyomi);
                    string localAssetFolder = $"{window.outputDirectory}/{targetFolder}".Replace("\\", "/");
                    if (!EnsureSingleAssetDirectory(localAssetFolder))
                    {
                        logEntries.Add("Folder create failed: " + localAssetFolder);
                        Debug.LogWarning("[KaleidoVR] Could not create output folder: " + localAssetFolder);
                        continue;
                    }

                    string targetPath = $"{window.outputDirectory}/{targetFolder}/{Path.GetFileName(path)}".Replace("\\", "/");
                    if (path.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        movedAssetsMap[path] = path;
                        continue;
                    }

                    if (action == "Copy")
                    {
                        // Never copy when this type (or the whole export list) is set to Move.
                        if (KaleidoAssetOrganizerHelpers.InferTransferAction(window.organizeOptions) == "Move")
                        {
                            action = "Move";
                        }
                    }

                    if (action == "Copy")
                    {
                        if (AssetDatabase.LoadMainAssetAtPath(targetPath) != null)
                        {
                            targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);
                        }
                        if (AssetDatabase.CopyAsset(path, targetPath))
                        {
                            copied++;
                            copiedAssetsMap[path] = targetPath;
                            copiedDestinations.Add(targetPath);
                            movedAssetsMap[path] = targetPath;
                            logEntries.Add("Copied: " + path + " -> " + targetPath);
                            if (IsModelFile(targetPath)) RemapModelImporterMaterials(targetPath, logEntries);
                        }
                        else
                        {
                            logEntries.Add("Copy failed: " + path);
                            Debug.LogWarning("[KaleidoVR] CopyAsset failed for " + path);
                        }
                    }
                    else if (action == "Move")
                    {
                        if (!TryMoveAsset(path, ref targetPath, logEntries))
                        {
                            logEntries.Add("Move failed: " + path);
                            Debug.LogWarning("[KaleidoVR] MoveAsset failed for " + path);
                        }
                        else
                        {
                            moved++;
                            movedAssetsMap[path] = targetPath;
                            processedPaths.Add(targetPath);
                            logEntries.Add("Moved: " + path + " -> " + targetPath);
                            if (IsModelFile(targetPath)) RemapModelImporterMaterials(targetPath, logEntries);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Only rewrite references on NEW copies. Moved files keep the same GUIDs,
                // and the selected object must keep pointing at those same assets.
                RemapCopiedAssetReferences(copiedAssetsMap, protectedAssetPaths);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                // =========================================================================
                // BLOCK 14: AUTOMATED PREFAB PACKAGING ENGINE LOOPS
                // =========================================================================

                string safePrefabName = KaleidoAssetOrganizerHelpers.SanitizeFileName(window.prefabName);
                if (string.IsNullOrEmpty(safePrefabName)) safePrefabName = "NewAvatar";
                string savedPrefabPath = null;
                GameObject finalTargetRoot = null;
                List<GameObject> instantiatedInstances = new List<GameObject>();

                try
                {
                    List<GameObject> activeTargetGameObjects = new List<GameObject>();
                    foreach (var obj in window.objectsToOrganize)
                    {
                        if (obj is GameObject go) activeTargetGameObjects.Add(go);
                    }

                    if (activeTargetGameObjects.Count == 1)
                    {
                        GameObject rootGo = ResolveOrganizedGameObject(activeTargetGameObjects[0], movedAssetsMap);
                        finalTargetRoot = InstantiateForPrefab(rootGo, protectedInstanceIds);
                        if (finalTargetRoot != null)
                        {
                            finalTargetRoot.name = safePrefabName;
                            instantiatedInstances.Add(finalTargetRoot);
                        }
                    }
                    else if (activeTargetGameObjects.Count > 1)
                    {
                        finalTargetRoot = new GameObject(safePrefabName);
                        foreach (var go in activeTargetGameObjects)
                        {
                            GameObject targetGo = ResolveOrganizedGameObject(go, movedAssetsMap);
                            GameObject instance = InstantiateForPrefab(targetGo, protectedInstanceIds);
                            if (instance != null)
                            {
                                instance.transform.SetParent(finalTargetRoot.transform);
                                instantiatedInstances.Add(instance);
                            }
                        }
                    }

                    if (finalTargetRoot != null && !IsProtectedObject(finalTargetRoot, protectedInstanceIds))
                    {
                        if (PrefabUtility.IsPartOfPrefabInstance(finalTargetRoot))
                        {
                            PrefabUtility.UnpackPrefabInstance(finalTargetRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        }

                        foreach (GameObject inst in instantiatedInstances)
                        {
                            if (inst != null && inst != finalTargetRoot && !IsProtectedObject(inst, protectedInstanceIds) && PrefabUtility.IsPartOfPrefabInstance(inst))
                            {
                                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                            }
                        }

                        Component[] allComponents = finalTargetRoot.GetComponentsInChildren<Component>(true);
                        foreach (Component comp in allComponents)
                        {
                            if (comp == null || IsProtectedObject(comp, protectedInstanceIds)) continue;
                            RemapSerializedReferences(comp, copiedAssetsMap);
                        }

                        if (window.autoSetupVRCDescriptor) ApplyVRCDescriptorSetup(finalTargetRoot, movedAssetsMap);

                        if (window.createPrefab)
                        {
                            string prefabFolder = $"{window.outputDirectory}/Prefabs".Replace("\\", "/");
                            if (!EnsureSingleAssetDirectory(prefabFolder))
                            {
                                logEntries.Add("Prefab folder create failed: " + prefabFolder);
                            }
                            else
                            {
                                string prefabPath = $"{prefabFolder}/{safePrefabName}.prefab";
                                string transferredPrefab = FindTransferredSourcePrefab(activeTargetGameObjects, movedAssetsMap);
                                bool transferredWasCopied = !string.IsNullOrEmpty(transferredPrefab) && copiedDestinations.Contains(transferredPrefab);
                                bool transferredWasMoved = !string.IsNullOrEmpty(transferredPrefab) && !transferredWasCopied;

                                if (transferredWasMoved)
                                {
                                    // Move keeps the same GUID. Do not rewrite the prefab the selection still instances.
                                    savedPrefabPath = transferredPrefab;
                                    logEntries.Add("Using moved prefab without rewriting it: " + transferredPrefab);
                                }
                                else
                                {
                                    if (transferredWasCopied)
                                    {
                                        prefabPath = transferredPrefab;
                                    }
                                    if (protectedAssetPaths.Contains(prefabPath) || (AssetDatabase.LoadMainAssetAtPath(prefabPath) != null && !transferredWasCopied))
                                    {
                                        prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{prefabFolder}/{safePrefabName}.prefab");
                                    }
                                    AssetDatabase.SaveAssets();
                                    PrefabUtility.SaveAsPrefabAsset(finalTargetRoot, prefabPath);
                                    savedPrefabPath = prefabPath;
                                    logEntries.Add("Prefab saved: " + prefabPath);
                                }
                            }
                        }

                        bool consumedWorkingRoot;
                        SaveOrganizedScene(window, finalTargetRoot, savedPrefabPath, safePrefabName, logEntries, out consumedWorkingRoot);
                        if (consumedWorkingRoot) finalTargetRoot = null;

                        Debug.Log($"[KaleidoVR] Pipeline operations finalized. Backup file records mapped independently.");
                    }
                }
                finally
                {
                    if (finalTargetRoot != null)
                    {
                        UnityEngine.Object.DestroyImmediate(finalTargetRoot);
                    }
                }
                Debug.Log($"[KaleidoVR] Pipeline Complete. Copied={copied}, Moved={moved}, Ignored={ignored}");
                logEntries.Add($"Pipeline Complete. Copied={copied}, Moved={moved}, Ignored={ignored}");

                List<string> leftoverSameNames = FindLeftoverSameNamedAssets(movedAssetsMap, window.outputDirectory);
                foreach (string leftover in leftoverSameNames)
                {
                    logEntries.Add("Left behind (not referenced by the selected object): " + leftover);
                }

                if (copied == 0 && moved == 0)
                {
                    EditorUtility.DisplayDialog(
                        "KaleidoVR Asset Organizer",
                        "The folder tree was created, but no assets were copied or moved.\n\nCheck that the selected object references Project assets (FBX, prefab, materials) and that the output folder is not the same folder those assets already live in.",
                        "OK");
                }
                else
                {
                    string leftoverText = leftoverSameNames.Count == 0
                        ? ""
                        : "\n\nThese same-named files were still in the original folders after Move. Drop the original avatar (not the organized result) if you want the whole pack relocated:\n- "
                          + string.Join("\n- ", leftoverSameNames.GetRange(0, Mathf.Min(8, leftoverSameNames.Count)));
                    if (leftoverSameNames.Count > 8)
                    {
                        leftoverText += "\n- ...";
                    }
                    EditorUtility.DisplayDialog(
                        "KaleidoVR Asset Organizer",
                        "Copied: " + copied + "\nMoved: " + moved + "\nIgnored: " + ignored
                        + (copied > 0 ? "\n\nCopy leaves the original in place. Set that type to Move if you want the source file to leave its folder." : "")
                        + leftoverText,
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KaleidoVR] Critical error during organization: " + ex);
                logEntries.Add("Critical error: " + ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                try
                {
                    File.WriteAllLines(logFile, logEntries.ToArray());
                }
                catch (Exception logEx)
                {
                    Debug.LogWarning("[KaleidoVR] Could not write organizer log: " + logEx.Message);
                }
            }
        }

        private static bool TryMoveAsset(string sourcePath, ref string targetPath, List<string> logEntries)
        {
            if (AssetDatabase.LoadMainAssetAtPath(targetPath) != null)
            {
                targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);
            }

            AssetDatabase.SaveAssets();
            string moveError = AssetDatabase.MoveAsset(sourcePath, targetPath);
            if (string.IsNullOrEmpty(moveError) && AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            {
                return true;
            }

            AssetDatabase.Refresh();
            if (AssetDatabase.LoadMainAssetAtPath(targetPath) != null)
            {
                targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);
            }

            moveError = AssetDatabase.MoveAsset(sourcePath, targetPath);
            if (string.IsNullOrEmpty(moveError) && AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            {
                return true;
            }

            if (string.IsNullOrEmpty(moveError) && AssetDatabase.LoadMainAssetAtPath(sourcePath) != null)
            {
                logEntries.Add("Move reported success but source still exists: " + sourcePath);
                return false;
            }

            logEntries.Add("Move failed: " + sourcePath + " (" + moveError + ")");
            return false;
        }

        private static List<string> FindLeftoverSameNamedAssets(Dictionary<string, string> transferred, string outputDirectory)
        {
            List<string> leftovers = new List<string>();
            if (transferred == null || transferred.Count == 0) return leftovers;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kvp in transferred)
            {
                string destPath = kvp.Value;
                if (string.IsNullOrEmpty(destPath)) continue;
                string fileName = Path.GetFileName(destPath);
                if (string.IsNullOrEmpty(fileName)) continue;
                string filter = Path.GetFileNameWithoutExtension(fileName);
                if (string.IsNullOrEmpty(filter)) continue;

                foreach (string guid in AssetDatabase.FindAssets(filter))
                {
                    string found = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(guid));
                    if (string.IsNullOrEmpty(found)) continue;
                    if (!string.Equals(Path.GetFileName(found), fileName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (KaleidoAssetOrganizerHelpers.IsSameOrInside(found, outputDirectory)) continue;
                    if (found.Equals(destPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (seen.Add(found)) leftovers.Add(found);
                }
            }
            leftovers.Sort(StringComparer.OrdinalIgnoreCase);
            return leftovers;
        }

        private static HashSet<int> BuildProtectedInstanceIds(List<UnityEngine.Object> selected)
        {
            HashSet<int> ids = new HashSet<int>();
            if (selected == null) return ids;
            foreach (UnityEngine.Object obj in selected)
            {
                if (obj == null) continue;
                GameObject go = obj as GameObject;
                if (go == null && obj is Component asComponent) go = asComponent.gameObject;
                if (go == null)
                {
                    ids.Add(obj.GetInstanceID());
                    continue;
                }
                ids.Add(go.GetInstanceID());
                Transform[] transforms = go.GetComponentsInChildren<Transform>(true);
                if (transforms != null)
                {
                    foreach (Transform childTransform in transforms)
                    {
                        if (childTransform != null) ids.Add(childTransform.gameObject.GetInstanceID());
                    }
                }
                Component[] childComponents = go.GetComponentsInChildren<Component>(true);
                if (childComponents != null)
                {
                    foreach (Component childComponent in childComponents)
                    {
                        if (childComponent != null) ids.Add(childComponent.GetInstanceID());
                    }
                }
            }
            return ids;
        }

        private static HashSet<string> BuildProtectedAssetPaths(List<UnityEngine.Object> selected)
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (selected == null) return paths;
            foreach (UnityEngine.Object obj in selected)
            {
                if (obj == null) continue;
                GameObject go = obj as GameObject;
                if (go == null && obj is Component component) go = component.gameObject;
                if (go != null)
                {
                    string path = ResolveGameObjectAssetPath(go);
                    if (!string.IsNullOrEmpty(path)) paths.Add(path);
                }
                string direct = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(direct)) paths.Add(KaleidoAssetOrganizerHelpers.NormalizeAssetPath(direct));
            }
            return paths;
        }

        private static bool IsProtectedObject(UnityEngine.Object obj, HashSet<int> protectedInstanceIds)
        {
            return obj != null && protectedInstanceIds != null && protectedInstanceIds.Contains(obj.GetInstanceID());
        }

        private static bool IsProtectedAssetPath(string path, HashSet<string> protectedAssetPaths)
        {
            path = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(path);
            return !string.IsNullOrEmpty(path) && protectedAssetPaths != null && protectedAssetPaths.Contains(path);
        }

        private static string FindTransferredSourcePrefab(List<GameObject> roots, Dictionary<string, string> movedAssetsMap)
        {
            if (roots == null || movedAssetsMap == null) return null;
            foreach (GameObject go in roots)
            {
                string sourcePath = ResolveGameObjectAssetPath(go);
                if (string.IsNullOrEmpty(sourcePath) || !sourcePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (movedAssetsMap.TryGetValue(sourcePath, out string destPath)
                    && !string.IsNullOrEmpty(destPath)
                    && destPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    return destPath;
                }
            }
            return null;
        }

        private static HashSet<string> CollectProjectAssetPaths(List<UnityEngine.Object> selected, List<UnityEngine.Object> ignoreList, List<string> logEntries)
        {
            HashSet<string> assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (selected == null) return assetPaths;

            foreach (UnityEngine.Object obj in selected)
            {
                if (obj == null || (ignoreList != null && ignoreList.Contains(obj))) continue;

                AddResolvedAssetPath(obj, assetPaths, logEntries);

                GameObject go = obj as GameObject;
                if (go == null && obj is Component component) go = component.gameObject;
                if (go == null) continue;

                string sourcePath = ResolveGameObjectAssetPath(go);
                if (!string.IsNullOrEmpty(sourcePath))
                {
                    AddAssetPath(sourcePath, assetPaths);
                    logEntries.Add("Resolved Hierarchy source: " + go.name + " -> " + sourcePath);
                }

                Component[] components = go.GetComponentsInChildren<Component>(true);
                List<UnityEngine.Object> harvestTargets = new List<UnityEngine.Object> { go };
                if (components != null)
                {
                    foreach (Component comp in components)
                    {
                        if (comp == null) continue;
                        harvestTargets.Add(comp);
                    }
                }

                foreach (UnityEngine.Object target in harvestTargets)
                {
                    HarvestSerializedAssetReferences(target, assetPaths);
                }

                try
                {
                    foreach (UnityEngine.Object collected in EditorUtility.CollectDependencies(harvestTargets.ToArray()))
                    {
                        AddResolvedAssetPath(collected, assetPaths, null);
                    }
                }
                catch (Exception)
                {
                }
            }

            string[] seeds = new string[assetPaths.Count];
            assetPaths.CopyTo(seeds);
            foreach (string seedPath in seeds)
            {
                if (string.IsNullOrEmpty(seedPath) || KaleidoAssetOrganizerHelpers.ShouldIgnoreAsset(seedPath)) continue;
                try
                {
                    foreach (string depPath in AssetDatabase.GetDependencies(seedPath, true))
                    {
                        AddAssetPath(depPath, assetPaths);
                    }
                }
                catch (Exception)
                {
                }
            }

            logEntries.Add("Project assets resolved: " + assetPaths.Count);
            return assetPaths;
        }

        private static bool IsSelectionAlreadyInOutput(List<UnityEngine.Object> selected, string outputDirectory)
        {
            if (selected == null) return false;
            foreach (UnityEngine.Object obj in selected)
            {
                GameObject go = obj as GameObject;
                if (go == null && obj is Component component) go = component.gameObject;
                string path = go != null ? ResolveGameObjectAssetPath(go) : AssetDatabase.GetAssetPath(obj);
                if (KaleidoAssetOrganizerHelpers.IsSameOrInside(path, outputDirectory)) return true;
            }
            return false;
        }

        private static void CollectMovePackAssets(KaleidoAssetOrganizer window, HashSet<string> assetPaths, List<string> logEntries)
        {
            if (window == null || assetPaths == null) return;

            HashSet<string> packRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string fromDeps = FindAvatarPackRoot(assetPaths);
            if (!string.IsNullOrEmpty(fromDeps)) packRoots.Add(fromDeps);

            if (window.objectsToOrganize != null)
            {
                foreach (UnityEngine.Object obj in window.objectsToOrganize)
                {
                    GameObject go = obj as GameObject;
                    if (go == null && obj is Component asComponent) go = asComponent.gameObject;
                    string sourcePath = go != null ? ResolveGameObjectAssetPath(go) : AssetDatabase.GetAssetPath(obj);
                    string dir = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(Path.GetDirectoryName(sourcePath));
                    while (!string.IsNullOrEmpty(dir) && KaleidoAssetOrganizerHelpers.IsInsideAssets(dir) && !dir.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                    {
                        if (LooksLikeAvatarPack(dir))
                        {
                            packRoots.Add(dir);
                            break;
                        }
                        int slash = dir.LastIndexOf('/');
                        if (slash <= 0) break;
                        dir = dir.Substring(0, slash);
                    }
                }
            }

            foreach (string packRoot in packRoots)
            {
                if (string.IsNullOrEmpty(packRoot) || packRoot.Equals("Assets", StringComparison.OrdinalIgnoreCase)) continue;
                if (KaleidoAssetOrganizerHelpers.IsSameOrInside(packRoot, window.outputDirectory)
                    || KaleidoAssetOrganizerHelpers.IsSameOrInside(window.outputDirectory, packRoot))
                {
                    logEntries.Add("Move pack sweep skipped: avatar folder is the output folder (" + packRoot + ").");
                    continue;
                }

                logEntries.Add("Move pack root: " + packRoot);
                string absolute = Path.GetFullPath(Path.Combine(GetProjectRootPath(), packRoot.Replace('/', Path.DirectorySeparatorChar)));
                if (!Directory.Exists(absolute)) continue;

                foreach (string file in Directory.GetFiles(absolute, "*.*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    string found = FilePathToAssetPath(file);
                    if (string.IsNullOrEmpty(found) || AssetDatabase.IsValidFolder(found)) continue;
                    if (KaleidoAssetOrganizerHelpers.ShouldIgnoreAsset(found)) continue;
                    UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(found);
                    string typeName = KaleidoAssetOrganizerHelpers.ResolveExportTypeName(found, mainAsset, mainAsset);
                    if (KaleidoAssetOrganizerHelpers.ResolveOrganizeAction(window.organizeOptions, typeName) != "Move") continue;
                    AddAssetPath(found, assetPaths);
                }
            }
        }

        private static string FindAvatarPackRoot(HashSet<string> assetPaths)
        {
            List<string> dirs = new List<string>();
            foreach (string path in assetPaths)
            {
                if (string.IsNullOrEmpty(path) || !KaleidoAssetOrganizerHelpers.IsInsideAssets(path)) continue;
                string dir = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(Path.GetDirectoryName(path));
                if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);
            }
            if (dirs.Count == 0) return null;

            string common = dirs[0];
            for (int i = 1; i < dirs.Count; i++)
            {
                common = CommonAssetFolder(common, dirs[i]);
                if (string.IsNullOrEmpty(common) || common.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            string current = common;
            string best = common;
            while (!string.IsNullOrEmpty(current) && KaleidoAssetOrganizerHelpers.IsInsideAssets(current) && !current.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                if (LooksLikeAvatarPack(current)) best = current;
                int slash = current.LastIndexOf('/');
                if (slash <= 0) break;
                current = current.Substring(0, slash);
            }
            return best;
        }

        private static string CommonAssetFolder(string a, string b)
        {
            a = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(a);
            b = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(b);
            string[] pa = a.Split('/');
            string[] pb = b.Split('/');
            int n = Math.Min(pa.Length, pb.Length);
            List<string> parts = new List<string>();
            for (int i = 0; i < n; i++)
            {
                if (!string.Equals(pa[i], pb[i], StringComparison.OrdinalIgnoreCase)) break;
                parts.Add(pa[i]);
            }
            if (parts.Count == 0) return null;
            return string.Join("/", parts.ToArray());
        }

        private static bool LooksLikeAvatarPack(string folder)
        {
            string[] markers = { "FBX", "Texture", "Textures", "Prefab", "Prefabs", "Materials", "VRC3.0", "VRC", "3.0", "Animation", "Animations" };
            int hits = 0;
            foreach (string marker in markers)
            {
                if (AssetDatabase.IsValidFolder(folder + "/" + marker)) hits++;
            }
            return hits >= 2;
        }

        private static int CompareTransferOrder(string a, string b)
        {
            int rankCmp = TransferRank(a).CompareTo(TransferRank(b));
            if (rankCmp != 0) return rankCmp;
            return StringComparer.OrdinalIgnoreCase.Compare(a, b);
        }

        private static int TransferRank(string path)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            if (IsModelFile(path) || path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return 2;
            if (path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".overrideController", StringComparison.OrdinalIgnoreCase)) return 1;
            return 0;
        }

        private static bool IsModelFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".blend", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".vrm", StringComparison.OrdinalIgnoreCase);
        }

        private static string FilePathToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;
            string abs = absolutePath.Replace("\\", "/");
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (abs.Equals(dataPath, StringComparison.OrdinalIgnoreCase)) return "Assets";
            if (!abs.StartsWith(dataPath + "/", StringComparison.OrdinalIgnoreCase)) return null;
            return KaleidoAssetOrganizerHelpers.NormalizeAssetPath("Assets" + abs.Substring(dataPath.Length));
        }

        private static void RemapModelImporterMaterials(string modelPath, List<string> logEntries)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) return;
            try
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
                if (logEntries != null) logEntries.Add("FBX material extract disabled: " + modelPath);
            }
            catch (Exception ex)
            {
                if (logEntries != null) logEntries.Add("FBX importer update skipped: " + modelPath + " (" + ex.Message + ")");
            }
        }

        private static string ResolveGameObjectAssetPath(GameObject go)
        {
            if (go == null) return null;

            string path = AssetDatabase.GetAssetPath(go);
            if (!string.IsNullOrEmpty(path)) return KaleidoAssetOrganizerHelpers.NormalizeAssetPath(path);

            try
            {
                path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (!string.IsNullOrEmpty(path)) return KaleidoAssetOrganizerHelpers.NormalizeAssetPath(path);
            }
            catch (Exception)
            {
            }

            UnityEngine.Object source = SafeGetPrefabAssetSource(go);
            path = AssetDatabase.GetAssetPath(source);
            if (!string.IsNullOrEmpty(path)) return KaleidoAssetOrganizerHelpers.NormalizeAssetPath(path);

            return null;
        }

        private static void AddResolvedAssetPath(UnityEngine.Object obj, HashSet<string> assetPaths, List<string> logEntries)
        {
            if (obj == null) return;

            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) && obj is GameObject go)
            {
                path = ResolveGameObjectAssetPath(go);
            }
            else if (string.IsNullOrEmpty(path) && obj is Component component)
            {
                path = ResolveGameObjectAssetPath(component.gameObject);
            }

            if (string.IsNullOrEmpty(path) && obj is GameObject or Component)
            {
                UnityEngine.Object source = SafeGetPrefabAssetSource(obj);
                path = AssetDatabase.GetAssetPath(source);
            }

            AddAssetPath(path, assetPaths);
        }

        private static void AddAssetPath(string path, HashSet<string> assetPaths)
        {
            path = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(path);
            if (string.IsNullOrEmpty(path)) return;
            if (KaleidoAssetOrganizerHelpers.ShouldIgnoreAsset(path)) return;
            if (!KaleidoAssetOrganizerHelpers.IsInsideAssets(path)) return;
            assetPaths.Add(path);
        }

        private static void HarvestSerializedAssetReferences(UnityEngine.Object target, HashSet<string> assetPaths)
        {
            if (target == null) return;

            AddResolvedAssetPath(target, assetPaths, null);

            try
            {
                SerializedObject serializedObject = new SerializedObject(target);
                SerializedProperty property = serializedObject.GetIterator();
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    enterChildren = true;
                    try
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (property.name == "m_Script") continue;
                        UnityEngine.Object referenced = property.objectReferenceValue;
                        if (referenced == null) continue;
                        AddResolvedAssetPath(referenced, assetPaths, null);
                    }
                    catch (Exception)
                    {
                        enterChildren = false;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private static UnityEngine.Object SafeGetPrefabAssetSource(UnityEngine.Object instanceOrAsset)
        {
            if (instanceOrAsset == null) return null;
            try
            {
                UnityEngine.Object original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instanceOrAsset);
                if (original != null) return original;
            }
            catch (Exception)
            {
            }

            try
            {
                UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(instanceOrAsset);
                if (source != null) return source;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static string SaveOrganizedScene(KaleidoAssetOrganizer window, GameObject workingRoot, string savedPrefabPath, string safePrefabName, List<string> logEntries, out bool consumedWorkingRoot)
        {
            consumedWorkingRoot = false;
            if (window == null) return null;

            string safeSceneName = KaleidoAssetOrganizerHelpers.SanitizeFileName(window.sceneName);
            if (string.IsNullOrEmpty(safeSceneName)) safeSceneName = "OrganizedScene";

            string outputFolder = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(window.outputDirectory);
            if (!EnsureSingleAssetDirectory(outputFolder))
            {
                logEntries.Add("Scene folder create failed: " + outputFolder);
                return null;
            }

            string scenePath = $"{outputFolder}/{safeSceneName}.unity";
            if (AssetDatabase.LoadMainAssetAtPath(scenePath) != null)
            {
                scenePath = AssetDatabase.GenerateUniqueAssetPath(scenePath);
            }

            Scene organizedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                GameObject sceneInstance = null;
                if (!string.IsNullOrEmpty(savedPrefabPath))
                {
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(savedPrefabPath);
                    if (prefabAsset != null)
                    {
                        sceneInstance = PrefabUtility.InstantiatePrefab(prefabAsset, organizedScene) as GameObject;
                    }
                }

                if (sceneInstance == null && workingRoot != null)
                {
                    EditorSceneManager.MoveGameObjectToScene(workingRoot, organizedScene);
                    sceneInstance = workingRoot;
                    consumedWorkingRoot = true;
                }

                if (sceneInstance != null)
                {
                    sceneInstance.name = string.IsNullOrEmpty(safePrefabName) ? safeSceneName : safePrefabName;
                }

                if (!EditorSceneManager.SaveScene(organizedScene, scenePath))
                {
                    logEntries.Add("Scene save failed: " + scenePath);
                    Debug.LogWarning("[KaleidoVR] Could not save organized scene: " + scenePath);
                    return null;
                }

                logEntries.Add("Scene saved: " + scenePath);
                return scenePath;
            }
            catch (Exception ex)
            {
                logEntries.Add("Scene save error: " + ex.Message);
                Debug.LogWarning("[KaleidoVR] Could not save organized scene: " + ex.Message);
                return null;
            }
            finally
            {
                if (organizedScene.IsValid())
                {
                    EditorSceneManager.CloseScene(organizedScene, true);
                }
            }
        }

        private static string GetProjectRootPath()
        {
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (dataPath.EndsWith("/Assets", StringComparison.OrdinalIgnoreCase))
            {
                return dataPath.Substring(0, dataPath.Length - "Assets".Length);
            }
            return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        }

        private static GameObject ResolveOrganizedGameObject(GameObject source, Dictionary<string, string> movedAssetsMap)
        {
            if (source == null) return null;

            string oldAssetPath = ResolveGameObjectAssetPath(source);

            if (!string.IsNullOrEmpty(oldAssetPath) && movedAssetsMap.TryGetValue(oldAssetPath, out string newPath))
            {
                GameObject updatedAssetSource = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
                if (updatedAssetSource != null) return updatedAssetSource;
            }

            return source;
        }

        private static GameObject TryInstantiatePrefab(UnityEngine.Object prefabAsset)
        {
            if (prefabAsset == null) return null;
            try
            {
                return PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static GameObject InstantiateForPrefab(GameObject source, HashSet<int> protectedInstanceIds)
        {
            if (source == null) return null;

            GameObject instance = null;
            if (PrefabUtility.IsPartOfPrefabAsset(source))
            {
                instance = TryInstantiatePrefab(source);
            }

            if (instance == null && PrefabUtility.IsPartOfPrefabInstance(source))
            {
                UnityEngine.Object prefabAsset = SafeGetPrefabAssetSource(source);
                instance = TryInstantiatePrefab(prefabAsset);
            }

            if (instance == null)
            {
                string assetPath = ResolveGameObjectAssetPath(source);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    UnityEngine.Object main = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    instance = TryInstantiatePrefab(main);
                }
            }

            if (instance == null || IsProtectedObject(instance, protectedInstanceIds) || instance == source)
            {
                instance = UnityEngine.Object.Instantiate(source);
            }

            return instance;
        }

        private static void RemapCopiedAssetReferences(Dictionary<string, string> copiedAssetsMap, HashSet<string> protectedAssetPaths)
        {
            if (copiedAssetsMap == null || copiedAssetsMap.Count == 0) return;

            HashSet<string> newPaths = new HashSet<string>(copiedAssetsMap.Values, StringComparer.OrdinalIgnoreCase);
            foreach (string newPath in newPaths)
            {
                if (IsProtectedAssetPath(newPath, protectedAssetPaths)) continue;
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(newPath);
                if (assets == null) continue;
                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset == null) continue;
                    RemapSerializedReferences(asset, copiedAssetsMap);
                }
            }
        }

        private static void RemapSerializedReferences(UnityEngine.Object target, Dictionary<string, string> movedAssetsMap)
        {
            if (target == null || movedAssetsMap == null || movedAssetsMap.Count == 0) return;

            try
            {
                SerializedObject serializedObject = new SerializedObject(target);
                SerializedProperty property = serializedObject.GetIterator();
                bool modified = false;
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    enterChildren = true;
                    try
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (property.name == "m_Script") continue;

                        UnityEngine.Object referenced = property.objectReferenceValue;
                        if (referenced == null) continue;

                        string referencedPath = AssetDatabase.GetAssetPath(referenced);
                        if (string.IsNullOrEmpty(referencedPath)) continue;
                        if (!movedAssetsMap.TryGetValue(referencedPath, out string remappedPath)) continue;
                        if (referencedPath.Equals(remappedPath, StringComparison.OrdinalIgnoreCase)) continue;

                        UnityEngine.Object remapped = AssetDatabase.LoadAssetAtPath(remappedPath, referenced.GetType());
                        if (remapped == null) remapped = AssetDatabase.LoadMainAssetAtPath(remappedPath);
                        if (remapped == null) continue;

                        property.objectReferenceValue = remapped;
                        modified = true;
                    }
                    catch (Exception)
                    {
                        enterChildren = false;
                    }
                }

                if (modified) serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception)
            {
            }
        }
        // =========================================================================
        // BLOCK 15: DESCRIPTOR LAYER AUTOMATION LINKS ENGINE
        // =========================================================================

        private static Type FindTypeByFullName(string fullName)
        {
            Type direct = Type.GetType(fullName + ", VRC.SDK3A") ?? Type.GetType(fullName);
            if (direct != null) return direct;

            Assembly[] assemblies;
            try
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return null;
            }

            foreach (var assembly in assemblies)
            {
                if (assembly == null) continue;
                try
                {
                    Type found = assembly.GetType(fullName, false);
                    if (found != null) return found;
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        private static bool IsFxAnimLayer(SerializedProperty typeProp)
        {
            if (typeProp == null) return false;
            try
            {
                if (typeProp.propertyType == SerializedPropertyType.Enum)
                {
                    string[] names = typeProp.enumNames;
                    int idx = typeProp.enumValueIndex;
                    if (names != null && idx >= 0 && idx < names.Length && names[idx] == "FX")
                    {
                        return true;
                    }
                }
                return typeProp.intValue == 5;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ApplyVRCDescriptorSetup(GameObject root, Dictionary<string, string> movedAssetsMap)
        {
            Type vrcDescriptorType = FindTypeByFullName("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (vrcDescriptorType == null)
            {
                Debug.LogWarning("[KaleidoVR] VRCAvatarDescriptor was not found. Install the VRChat Avatars SDK to auto-link FX & Menu.");
                return;
            }

            Component descriptor = root.GetComponent(vrcDescriptorType) ?? root.AddComponent(vrcDescriptorType);
            var expressionsMenuType = FindTypeByFullName("VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu");
            var expressionParamsType = FindTypeByFullName("VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters");

            RuntimeAnimatorController targetFXController = null;
            UnityEngine.Object targetMenu = null;
            UnityEngine.Object targetParams = null;
            foreach (var kvp in movedAssetsMap)
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(kvp.Value);
                if (asset is RuntimeAnimatorController rac && rac.name.ToLowerInvariant().Contains("fx"))
                {
                    targetFXController = rac;
                }
                else if (asset != null && expressionsMenuType != null && expressionsMenuType.IsAssignableFrom(asset.GetType()))
                {
                    targetMenu = asset;
                }
                else if (asset != null && expressionParamsType != null && expressionParamsType.IsAssignableFrom(asset.GetType()))
                {
                    targetParams = asset;
                }
            }

            try
            {
                SerializedObject descriptorObject = new SerializedObject(descriptor);
                SerializedProperty menuProp = descriptorObject.FindProperty("expressionsMenu");
                SerializedProperty paramsProp = descriptorObject.FindProperty("expressionParameters");
                if (menuProp != null && targetMenu != null) menuProp.objectReferenceValue = targetMenu;
                if (paramsProp != null && targetParams != null) paramsProp.objectReferenceValue = targetParams;

                SerializedProperty layers = descriptorObject.FindProperty("baseAnimationLayers");
                if (layers != null && targetFXController != null)
                {
                    for (int i = 0; i < layers.arraySize; i++)
                    {
                        SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                        SerializedProperty typeProp = layer.FindPropertyRelative("type");
                        bool isFx = IsFxAnimLayer(typeProp);

                        if (!isFx && i != 4) continue;
                        if (!isFx && i == 4 && layers.arraySize != 5) continue;

                        SerializedProperty isDefaultProp = layer.FindPropertyRelative("isDefault");
                        SerializedProperty controllerProp = layer.FindPropertyRelative("animatorController");
                        SerializedProperty enabledProp = layer.FindPropertyRelative("isEnabled");
                        if (isDefaultProp != null) isDefaultProp.boolValue = false;
                        if (enabledProp != null) enabledProp.boolValue = true;
                        if (controllerProp != null) controllerProp.objectReferenceValue = targetFXController;
                        if (isFx) break;
                    }
                }

                descriptorObject.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception ex) { Debug.LogWarning("[KaleidoVR] Reflection error: " + ex.Message); }
        }

        private static bool PrepareOutputFolders(string outputDirectory, bool parsePoiyomi, List<string> logEntries)
        {
            List<string> folders = new List<string>
            {
                outputDirectory,
                outputDirectory + "/FBX",
                outputDirectory + "/Materials",
                outputDirectory + "/Textures",
                outputDirectory + "/Audio",
                outputDirectory + "/Prefabs",
                outputDirectory + "/Other",
                outputDirectory + "/3.0",
                outputDirectory + "/3.0/Animations",
                outputDirectory + "/3.0/BlendTrees",
                outputDirectory + "/3.0/Avatar Masks",
                outputDirectory + "/3.0/Controllers",
                outputDirectory + "/3.0/Menus",
                outputDirectory + "/3.0/VRCExpressionParameters"
            };

            if (parsePoiyomi)
            {
                folders.Add(outputDirectory + "/Textures/Normals");
                folders.Add(outputDirectory + "/Textures/Emissions");
                folders.Add(outputDirectory + "/Textures/Metallic");
                folders.Add(outputDirectory + "/Textures/Roughness");
                folders.Add(outputDirectory + "/Textures/AO");
            }

            foreach (string folder in folders)
            {
                if (!EnsureSingleAssetDirectory(folder))
                {
                    logEntries.Add("Folder create failed: " + folder);
                    return false;
                }
            }
            return true;
        }

        // Create the exact folder path. Never use CreateFolder — if the name already
        // exists on disk, Unity invents "Models 1", "Models 2", and so on.
        public static bool EnsureSingleAssetDirectory(string targetFolderPath)
        {
            string normalized = KaleidoAssetOrganizerHelpers.NormalizeAssetPath(targetFolderPath);
            if (string.IsNullOrEmpty(normalized)) return false;

            if (!KaleidoAssetOrganizerHelpers.IsInsideAssets(normalized))
            {
                Debug.LogWarning("[KaleidoVR] Output folder must be inside Assets: " + targetFolderPath);
                return false;
            }

            if (normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase)) return true;
            if (AssetDatabase.IsValidFolder(normalized)) return true;

            string projectRoot = GetProjectRootPath();
            string assetsAbsolute = Path.GetFullPath(Path.Combine(projectRoot, "Assets"));
            string folderAbsolute = Path.GetFullPath(Path.Combine(projectRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
            string assetsPrefix = assetsAbsolute.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!folderAbsolute.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[KaleidoVR] Refusing to create a folder outside Assets: " + normalized);
                return false;
            }

            if (!Directory.Exists(folderAbsolute))
            {
                Directory.CreateDirectory(folderAbsolute);
            }

            string current = "Assets";
            string[] parts = normalized.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                current += "/" + parts[i];
                if (AssetDatabase.IsValidFolder(current)) continue;

                AssetDatabase.ImportAsset(current, ImportAssetOptions.ForceSynchronousImport);
                if (!AssetDatabase.IsValidFolder(current))
                {
                    AssetDatabase.Refresh();
                }
                if (!AssetDatabase.IsValidFolder(current))
                {
                    Debug.LogWarning("[KaleidoVR] Could not register folder with Unity: " + current);
                    return false;
                }
            }

            return AssetDatabase.IsValidFolder(normalized);
        }
    }
}
// =========================================================================
// BLOCK 16: USER INTERFACE LIST SCRIPT SORTERS & OBJECT DROP GRIDS
// =========================================================================

namespace KaleidoVR.EditorTools
{
    public static class KaleidoAssetOrganizerUI
    {
        private static readonly string[] organizeActions = new string[] { "Copy", "Move", "Ignore" };

        public static void DrawHeader(KaleidoAssetOrganizer window, Texture2D logo, string version)
        {
            GUIStyle centeredTitleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            GUIStyle centeredVersionStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Space(10); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (logo != null) { Rect logoRect = GUILayoutUtility.GetRect(320, 200, GUILayout.Width(320), GUILayout.Height(200)); GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit); }
            else { GUILayout.Label($"...Place your logo at {KaleidoAssetOrganizer.ICON_PATH}...", EditorStyles.miniLabel); }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); GUILayout.Space(2);
            GUILayout.Label("KALEIDO VR ORGANIZER", centeredTitleStyle); GUILayout.Label($"v{version}", centeredVersionStyle);
        }

        public static void DrawOutputDirectory(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Output Directory", EditorStyles.boldLabel); EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true); EditorGUILayout.TextField(window.outputDirectory, GUILayout.ExpandWidth(true)); EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    string dataPath = Application.dataPath.Replace("\\", "/");
                    string picked = path.Replace("\\", "/");
                    if (picked.Equals(dataPath, StringComparison.OrdinalIgnoreCase))
                    {
                        EditorUtility.DisplayDialog("KaleidoVR Asset Organizer", "Pick a folder under Assets, not the Assets root.", "OK");
                    }
                    else if (picked.StartsWith(dataPath + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        window.outputDirectory = KaleidoAssetOrganizerHelpers.NormalizeAssetPath("Assets" + picked.Substring(dataPath.Length));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("KaleidoVR Asset Organizer", "Pick a folder inside this project's Assets folder.", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawSettings(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Settings & Automation Pipelines", EditorStyles.boldLabel);
            window.sceneName = EditorGUILayout.TextField("Scene Name", window.sceneName);
            window.prefabName = EditorGUILayout.TextField("Prefab Name", window.prefabName);

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 180f;

            window.createPrefab = EditorGUILayout.Toggle("Create Prefab", window.createPrefab);

            EditorGUI.BeginDisabledGroup(!window.createPrefab);
            window.autoParsePoiyomi = EditorGUILayout.Toggle("Texture Sorter", window.autoParsePoiyomi);
            window.autoSetupVRCDescriptor = EditorGUILayout.Toggle("Auto-Link FX & Menu", window.autoSetupVRCDescriptor);
            EditorGUI.EndDisabledGroup();

            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        public static void DrawObjectsToOrganize(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Objects to Organize", EditorStyles.boldLabel);
            Rect dropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true)); GUI.Box(dropArea, "Drag & Drop Objects Here", EditorStyles.helpBox);
            int countBeforeDrop = window.objectsToOrganize.Count; KaleidoAssetOrganizerHelpers.HandleDragAndDrop(dropArea, window.objectsToOrganize);

            if (window.objectsToOrganize.Count > countBeforeDrop && window.objectsToOrganize.Count > 0 && window.objectsToOrganize[countBeforeDrop] != null)
            {
                if (window.sceneName == "OrganizedScene" || string.IsNullOrEmpty(window.sceneName) || window.prefabName == "NewAvatar" || string.IsNullOrEmpty(window.prefabName))
                {
                    string primaryName = window.objectsToOrganize[countBeforeDrop].name;
                    window.sceneName = primaryName + "_Scene";
                    window.prefabName = primaryName;
                }
            }

            for (int i = 0; i < window.objectsToOrganize.Count; i++)
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal(); if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(rowRect, i % 2 == 0 ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.23f, 0.23f, 0.23f, 1f));
                GUILayout.Space(5); UnityEngine.Object oldObj = window.objectsToOrganize[i]; window.objectsToOrganize[i] = EditorGUILayout.ObjectField(window.objectsToOrganize[i], typeof(UnityEngine.Object), true, GUILayout.ExpandWidth(true));

                Event evt = Event.current;
                if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && rowRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        // FIXED: Grab index 0 item from the drag array reference to fix the variable mapping mismatch
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            window.objectsToOrganize[i] = DragAndDrop.objectReferences[0];
                            GUI.changed = true;
                        }
                    }
                    evt.Use();
                }
                if (i == 0 && window.objectsToOrganize[i] != oldObj && window.objectsToOrganize[i] != null) { string primaryName = window.objectsToOrganize[i].name; window.sceneName = primaryName + "_Scene"; window.prefabName = primaryName; }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    window.objectsToOrganize.RemoveAt(i); i--;
                    if (window.objectsToOrganize.Count == 0) { window.sceneName = "OrganizedScene"; window.prefabName = "NewAvatar"; }
                    else if (i < 0 && window.objectsToOrganize.Count > 0 && window.objectsToOrganize != null) { string primaryName = window.objectsToOrganize[0].name; window.sceneName = primaryName + "_Scene"; window.prefabName = primaryName; }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal(); GUILayout.Space(5); if (GUILayout.Button("+", GUILayout.Width(35))) window.objectsToOrganize.Add(null);
            if (GUILayout.Button("-", GUILayout.Width(35)) && window.objectsToOrganize.Count > 0) { window.objectsToOrganize.RemoveAt(window.objectsToOrganize.Count - 1); if (window.objectsToOrganize.Count == 0) { window.sceneName = "OrganizedScene"; window.prefabName = "NewAvatar"; } }
            GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();
        }

        public static void DrawOrganizeOptions(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Export List Options", EditorStyles.boldLabel);
            foreach (var key in new List<string>(window.organizeOptions.Keys))
            {
                EditorGUILayout.BeginHorizontal(); GUIContent rowContent = new GUIContent($" {FriendlyName(key)}", GetNativeUnityIcon(key)); GUILayout.Label(rowContent, GUILayout.Height(18), GUILayout.Width(240)); GUILayout.FlexibleSpace();
                int currentIndex = Array.IndexOf(organizeActions, window.organizeOptions[key]); int selectedIndex = EditorGUILayout.Popup(currentIndex < 0 ? 0 : currentIndex, organizeActions, GUILayout.Width(90)); window.organizeOptions[key] = organizeActions[selectedIndex]; EditorGUILayout.EndHorizontal();
            }
        }
        // =========================================================================
        // BLOCK 17: NATIVE SKIN ICON TRANSLATORS & ACTION LINK PORTALS
        // =========================================================================

        private static Texture GetNativeUnityIcon(string typeName)
        {
            string iconName = typeName switch { "GameObject" => "Prefab Icon", "Texture2D" => "Texture2D Icon", "Cubemap" => "Cubemap Icon", "Material" => "Material Icon", "VRCExpressionParameters" => "ScriptableObject Icon", "VRCExpressionsMenu" => "ScriptableObject Icon", "BlendTree" => "BlendTree Icon", "AnimationClip" => "AnimationClip Icon", "AnimatorOverrideController" => "AnimatorOverrideController Icon", "AnimatorController" => "AnimatorController Icon", "AvatarMask" => "AvatarMask Icon", "AudioClip" => "AudioClip Icon", "Shader" => "Shader Icon", "MonoScript" => "cs Script Icon", _ => "DefaultAsset Icon" };
            try
            {
                GUIContent content = EditorGUIUtility.IconContent(iconName);
                return content != null ? content.image : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FriendlyName(string typeName)
        {
            return typeName switch { "GameObject" => "GameObject (FBX)", "Texture2D" => "Texture2D Maps", "VRCExpressionParameters" => "VRCExpressionParameters", "VRCExpressionsMenu" => "VRCExpressionsMenu", "MonoScript" => "MonoScript (C#)", "DefaultAsset" => "DefaultAsset (DLLs)", _ => typeName };
        }

        public static void DrawIgnoreList(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Ignore List (Manually Exclude Objects)", EditorStyles.boldLabel); Rect ignoreDropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true)); GUI.Box(ignoreDropArea, "Drag & Drop Objects to Exclude", EditorStyles.helpBox); KaleidoAssetOrganizerHelpers.HandleDragAndDrop(ignoreDropArea, window.ignoreList);
            for (int i = 0; i < window.ignoreList.Count; i++)
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal(); window.ignoreList[i] = EditorGUILayout.ObjectField(window.ignoreList[i], typeof(UnityEngine.Object), true, GUILayout.ExpandWidth(true));
                Event evt = Event.current;
                if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && rowRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        // FIXED: Grab index 0 item from the drag array reference to prevent structural mapping crashes
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            window.ignoreList[i] = DragAndDrop.objectReferences[0];
                            GUI.changed = true;
                        }
                    }
                    evt.Use();
                }
                if (GUILayout.Button("X", GUILayout.Width(25))) { window.ignoreList.RemoveAt(i); i--; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal(); GUILayout.Space(5); if (GUILayout.Button("+", GUILayout.Width(35))) window.ignoreList.Add(null); GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();
        }

        public static void DrawOrganizeButton(KaleidoAssetOrganizer window) { if (GUILayout.Button("Organize Assets", GUILayout.Height(35))) KaleidoAssetOrganizerLogic.OrganizeAssets(window); }
        public static void DrawDiscordButton() { GUILayout.Space(2); if (GUILayout.Button("💬 Join the Discord Server", GUILayout.Height(24))) Application.OpenURL("https://discord.com/invite/cRsufJssTA"); }
        public static void DrawFooter(KaleidoAssetOrganizer window) { GUILayout.Space(5); if (GUILayout.Button("Visit kalivr.com", EditorStyles.centeredGreyMiniLabel)) Application.OpenURL("https://kalivr.com"); }
    }
}