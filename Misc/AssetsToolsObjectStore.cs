using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace LunarUploader.Misc {
    internal class AssetsToolsObjectStore
    {
        private readonly AssetsManager _assetsManager;
        private AssetsFileInstance _currentFile;
        private BundleFileInstance _currentBundle;
        private AssetBundleFile _currentUnpackedFile;
        private readonly List<AssetsReplacer> _assetsReplacers;
 
        private readonly string _assetsFilePath;
        private readonly string _oldId;
        private readonly string _newId;
        private readonly AssetsToolsObjectType _type;

        private string _bundleName;
        private string _NewbundleName;
        private readonly List<string> _oldIdsInFile;
        private string _oldIdInFile;
        private readonly bool _saveToFile;

        internal bool Error { get; private set; }
        internal Exception LastError { get; private set; }
        internal MemoryStream AssetsFile { get; private set; }
        internal string AssetsFilePath { get; private set; }

        public AssetsToolsObjectStore(string path, string oldId, string newId, AssetsToolsObjectType type, bool saveToFile = true, CancellationToken? ct = null) 
        {
            if (string.IsNullOrEmpty(path)) {
                var ex = new ArgumentNullException(nameof(path));
                SetError(ex);
                throw ex;
            }
            if (string.IsNullOrEmpty(newId)) {
                var ex = new ArgumentNullException(nameof(newId));
                SetError(ex);
                throw ex;
            }
            if (type is AssetsToolsObjectType.None) {
                var ex = new ArgumentNullException(nameof(type));
                SetError(ex);
                throw ex;
            }

            _assetsFilePath = path;
            _oldId = oldId;
            _newId = newId;
            _type = type;
            _saveToFile = saveToFile;

            _bundleName = string.Empty;
            _oldIdsInFile = new List<string>(20);
            _oldIdInFile = string.Empty;
            
            _assetsManager = new AssetsManager();
            _assetsManager.LoadClassPackage("classdata.tpk");
            _assetsReplacers = new List<AssetsReplacer>(20);

            _NewbundleName = $"CAB-{Guid.NewGuid():N}";
        }

        internal void LoadAndUnpack() 
        {
            try 
            {
                _currentBundle = _assetsManager.LoadBundleFile(_assetsFilePath, false);
                _currentUnpackedFile = _currentBundle.file.bundleHeader6.GetCompressionType() != 0 ? BundleHelper.UnpackBundle(_currentBundle.file) : _currentBundle.file;
                AssetsFileReader reader = _currentUnpackedFile.reader;
                AssetBundleDirectoryInfo06[] dirInf = _currentUnpackedFile.bundleInf6.dirInf;
                byte[] targetAssetsFile = null;

                if (_type is AssetsToolsObjectType.Avatar) 
                {
                    for (int i = 0; i < dirInf.Length; i++) 
                    {
                        var entry = dirInf[i];
                        if (!_currentUnpackedFile.IsAssetsFile(reader, entry) || !string.IsNullOrEmpty(Path.GetExtension(entry.name))) continue;
                        targetAssetsFile = BundleHelper.LoadAssetDataFromBundle(_currentUnpackedFile, i);
                        _bundleName = entry.name;
                        break;
                    }
                } 
                else if (_type is AssetsToolsObjectType.World) 
                {
                    for (int i = 0; i < dirInf.Length; i++) 
                    {
                        var entry = dirInf[i];
                        if (string.IsNullOrEmpty(Path.GetExtension(entry.name)) && (targetAssetsFile is null or { Length: <= 0 } || string.IsNullOrEmpty(_bundleName))) 
                        {
                            targetAssetsFile = BundleHelper.LoadAssetDataFromBundle(_currentUnpackedFile, i);
                            _bundleName = entry.name;
                            continue;
                        }

                        if (!_currentUnpackedFile.IsAssetsFile(reader, entry) || !entry.name.EndsWith("sharedAssets")) continue;

                        try 
                        {
                            var assetData = BundleHelper.LoadAssetDataFromBundle(_currentUnpackedFile, i);
                            if (assetData is null or { Length: <= 0 }) 
                            {
                                SetError(new NullReferenceException($"Couldn't load assets file."));
                                continue;
                            }

                            var afi = _assetsManager.LoadAssetsFile(new MemoryStream(assetData), entry.name, true);
                            afi.parentBundle = _currentBundle;
                        } 
                        catch (Exception e) 
                        {
                            Console.WriteLine(e);
                        }
                    }
                }

                if (targetAssetsFile is null or {Length: <= 0} || string.IsNullOrEmpty(_bundleName)) 
                {
                    SetError(new NullReferenceException($"Couldn't find matching assets file or directory name was null or empty."));
                    return;
                }

                Console.WriteLine($"Target assets file size: {(targetAssetsFile.Length / 1024) / 1024} MB");
                var mainAfi = _assetsManager.LoadAssetsFile(new MemoryStream(targetAssetsFile), _bundleName, true);
                mainAfi.parentBundle = _currentBundle;
                LoadMainAssetsFile(mainAfi);
            }
            catch (Exception e) 
            {
                Console.WriteLine(e);
                SetError(e);
            }

            //GCHelper.BlockingCollect();
        }

        private void LoadMainAssetsFile(AssetsFileInstance instance) 
        {
            if (Error) return;

            try 
            {
                if (_currentFile == null) 
                {
                    instance.table.GenerateQuickLookupTree();
                    _assetsManager.UpdateDependencies();
                    _assetsManager.LoadClassDatabaseFromPackage(instance.file.typeTree.unityVersion);
                    if (_assetsManager.classFile == null) {
                        ClassDatabaseFile[] files = _assetsManager.classPackage.files;
                        _assetsManager.classFile = files[files.Length - 1];
                    }

                    _currentFile = instance;
                }
            } catch (Exception e) 
            {
                Console.WriteLine(e);
                SetError(e);
            }
        }

        internal void ReplaceAvatarId() 
        {
            if (Error) return;

            var assetFileInfosEx = _currentFile.table.assetFileInfo;
            if (assetFileInfosEx is null or {Length: <= 0}) {
                SetError(new NullReferenceException("AssetsFileInfos is null or empty."));
                return;
            }

            var monoBehaviours = new List<AssetFileInfoEx>();
            var gameObjects = new List<AssetFileInfoEx>();
            var assetBundles = new List<AssetFileInfoEx>();

            for (int i = 0; i < assetFileInfosEx.Length; i++) 
            {
                var assetFileInfoEx = assetFileInfosEx[i];
                try 
                {
                    var cdbType = AssetHelper.FindAssetClassByID(_assetsManager.classFile, assetFileInfoEx.curFileType);
                    if (cdbType == null) continue;

                    var name = AssetHelper.GetAssetNameFast(_currentFile.file, _assetsManager.classFile, assetFileInfoEx);
                    if (string.IsNullOrEmpty(name)) continue;

                    var typeName = cdbType.name.GetString(_assetsManager.classFile);
                    if (string.IsNullOrEmpty(typeName)) continue;

                    if (typeName == "MonoBehaviour") monoBehaviours.Add(assetFileInfoEx);
                    else if (typeName == "GameObject" && name.StartsWith("prefab-")) gameObjects.Add(assetFileInfoEx);
                    else if (typeName == "AssetBundle" && name.StartsWith("prefab-")) assetBundles.Add(assetFileInfoEx);
                }
                catch (Exception e) 
                {
                    Console.WriteLine(e);
                    SetError(e);
                }
            }

            if (monoBehaviours.Count > 0) ProcessMonoBehaviours(monoBehaviours);

            if (gameObjects.Count > 0) ProcessGameObjects(gameObjects);

            if (assetBundles.Count > 0) ProcessAssetBundles(assetBundles);
        }

        private void ProcessMonoBehaviours(List<AssetFileInfoEx> monoBehaviours) 
        {
            if (Error) return;
            for (int i = 0; i < monoBehaviours.Count; i++) ProcessMonoBehaviour(monoBehaviours[i]);
        }

        private void ProcessGameObjects(List<AssetFileInfoEx> gameObjects) 
        {
            if (Error) return;
            for (int i = 0; i < gameObjects.Count; i++) ProcessAvatarPrefab(gameObjects[i]);
        }

        private void ProcessAssetBundles(List<AssetFileInfoEx> assetBundles) 
        {
            if (Error) return;

            for (int i = 0; i < assetBundles.Count; i++)
                ProcessAvatarAssetBundle(assetBundles[i]);
        }

        private void ProcessMonoBehaviour(AssetFileInfoEx assetFileInfoEx) 
        {
            if (Error)  return;

            try 
            {
                AssetPPtr ptr = new AssetPPtr(0, assetFileInfoEx.index);
                AssetExternal ext = _assetsManager.GetExtAsset(_currentFile, 0, ptr.pathID, false, true);
                if (ext.Equals(default(AssetExternal))) return;

                AssetTypeInstance scriptAti = _assetsManager.GetExtAsset(_currentFile, ext.instance.GetBaseField().Get("m_Script")).instance;
                if (scriptAti == null || scriptAti.Equals(default(AssetTypeInstance))) return;

                string className = scriptAti.GetBaseField().Get("m_Name").GetValue().AsString();
                if (string.IsNullOrEmpty(className)) return;

                if (className == "PipelineManager") ProcessPipelineManager(assetFileInfoEx);
            } 
            catch (Exception e)
            {
                Console.WriteLine(e);
                SetError(e);
            }
        }

        private void ProcessPipelineManager(AssetFileInfoEx assetFileInfoEx) 
        {
            if (Error) return;

            try {
                var baseField = _assetsManager.GetTypeInstance(_currentFile, assetFileInfoEx).GetBaseField();
                var baseFieldValue = baseField.Get("blueprintId").GetValue();
                var currentBlueprintId = baseFieldValue.AsString();
                _oldIdsInFile.Add(currentBlueprintId);
                if (!string.IsNullOrEmpty(_oldId) && currentBlueprintId != _oldId) Console.WriteLine($"[PPM] Given blueprintId ({_oldId}) doesn't match blueprintId ({currentBlueprintId}) in file.");
                baseFieldValue.Set(_newId);
                Console.WriteLine($"[PPM] Replaced blueprintId[{_oldIdsInFile.Count}] ({currentBlueprintId}) in file with new blueprintId {_newId}");
                var replacer = new AssetsReplacerFromMemory(0, assetFileInfoEx.index, (int)assetFileInfoEx.curFileType, _currentFile.file.typeTree.unity5Types[assetFileInfoEx.curFileTypeOrIndex].scriptIndex, baseField.WriteToByteArray());
                _assetsReplacers.Add(replacer);
            }
            catch (Exception e) 
            {
                Console.WriteLine(e);
                SetError(e);
            }
        }

        private void ProcessAvatarPrefab(AssetFileInfoEx assetFileInfoEx)
        {
            if (Error) return;

            try 
            {
                var baseField = _assetsManager.GetTypeInstance(_currentFile, assetFileInfoEx).GetBaseField();
                var baseFieldValue = baseField.Get("m_Name").GetValue();
                var prefabName = baseFieldValue.AsString();
                if (string.IsNullOrEmpty(_oldIdInFile)) 
                {
                    foreach (var oldId in _oldIdsInFile)
                    {
                        if (!prefabName.Contains(oldId.ToLower())) continue;
                        _oldIdInFile = oldId;
                        break;
                    }
                }
                var replacedPrefabName = Regex.Replace(prefabName, string.IsNullOrEmpty(_oldId) ? _oldIdInFile : _oldId, _newId, RegexOptions.IgnoreCase);

                if (prefabName == replacedPrefabName)
                {
                    Console.WriteLine("Unusual avatar detected. The prefab name could not be changed, this avatar might not be reuploadable. Switching to fallback method..");
                    var tempRegex = Regex.Match(prefabName, @"[_](.*)[_]\d*", RegexOptions.IgnoreCase);
                    if (!tempRegex.Success || tempRegex.Groups?.Count <= 1)
                    {
                        SetError(new InvalidOperationException("Fallback prefab name replacement failed"));
                        return;
                    }

                    replacedPrefabName = Regex.Replace(prefabName, tempRegex.Groups[1].Value, _newId, RegexOptions.IgnoreCase);
                }

                baseFieldValue.Set(replacedPrefabName);
                Console.WriteLine($"[PAP] Replaced name ({prefabName}) in file with {replacedPrefabName}");
                var replacer = new AssetsReplacerFromMemory(0, assetFileInfoEx.index, (int)assetFileInfoEx.curFileType, /*assetFileInfoEx.scriptIndex*/0xffff, baseField.WriteToByteArray());
                _assetsReplacers.Add(replacer);
            } catch (Exception e) {
                Console.WriteLine(e);
                SetError(e);
            }
        }

        private void ProcessAvatarAssetBundle(AssetFileInfoEx assetFileInfoEx)
        {
            if (Error) return;

            try 
            {
                var baseField = _assetsManager.GetTypeInstance(_currentFile, assetFileInfoEx).GetBaseField();
                var nameBaseFieldValue = baseField.Get("m_Name").GetValue();
                var name = nameBaseFieldValue.AsString();
                if (string.IsNullOrEmpty(_oldIdInFile)) 
                {
                    foreach (var oldId in _oldIdsInFile)
                    {
                        if (!name.Contains(oldId.ToLower())) continue;
                        _oldIdInFile = oldId;
                        break;
                    }
                }
                var id = string.IsNullOrEmpty(_oldId) ? _oldIdInFile : _oldId;
                var replacedName = Regex.Replace(name, id, _newId, RegexOptions.IgnoreCase);

                if (name == replacedName) 
                {
                    Console.WriteLine("Unusual avatar detected. The asset bundle name (m_Name) could not be changed, this avatar might not be reuploadable. Switching to fallback method..");
                    var tempRegex = Regex.Match(name, @"[_](.*)[_]\d*", RegexOptions.IgnoreCase);
                    if (!tempRegex.Success || tempRegex.Groups?.Count <= 1) {
                        Console.WriteLine("Fallback asset bundle name (m_Name) replacement failed");
                        return;
                    }

                    replacedName = Regex.Replace(name, tempRegex.Groups[1].Value, _newId, RegexOptions.IgnoreCase);
                }

                nameBaseFieldValue.Set(replacedName);
                Console.WriteLine($"[PAAB] Replaced name ({name}) in file with {replacedName}");

                var assetBundleNameBaseFieldValue = baseField.Get("m_AssetBundleName").GetValue();
                var assetBundleName = assetBundleNameBaseFieldValue.AsString();
                var replacedAssetBundleName = Regex.Replace(assetBundleName, id, _newId, RegexOptions.IgnoreCase);

                if (assetBundleName == replacedAssetBundleName) 
                {
                    Console.WriteLine("Unusual avatar detected. The asset bundle name (m_AssetBundleName) could not be changed, this avatar might not be reuploadable. Switching to fallback method..");
                    var tempRegex = Regex.Match(assetBundleName, @"[_](.*)[_]\d*", RegexOptions.IgnoreCase);
                    if (!tempRegex.Success || tempRegex.Groups?.Count <= 1) {
                        Console.WriteLine("Fallback asset bundle name (m_AssetBundleName) replacement failed");
                        return;
                    }

                    replacedAssetBundleName = Regex.Replace(assetBundleName, tempRegex.Groups[1].Value, _newId, RegexOptions.IgnoreCase);
                }

                assetBundleNameBaseFieldValue.Set(replacedAssetBundleName);
                Console.WriteLine($"[PAAB] Replaced name ({assetBundleName}) in file with {replacedAssetBundleName}");

                var containerFirstBaseFieldValue = baseField.Get("m_Container").Get("Array")[0].Get("first").GetValue();
                var path = containerFirstBaseFieldValue.AsString();
                var replacedPath = Regex.Replace(path, id, _newId, RegexOptions.IgnoreCase);

                if (path == replacedPath)
                {
                    Console.WriteLine("Unusual avatar detected. The asset bundle path could not be changed, this avatar might not be reuploadable. Switching to fallback method..");
                    var tempRegex = Regex.Match(path, @"[_](.*)[_]\d*", RegexOptions.IgnoreCase);
                    if (!tempRegex.Success || tempRegex.Groups?.Count <= 1) {
                        Console.WriteLine("Fallback asset bundle path replacement failed");
                        return;
                    }

                    replacedPath = Regex.Replace(path, tempRegex.Groups[1].Value, _newId, RegexOptions.IgnoreCase);
                }

                containerFirstBaseFieldValue.Set(replacedPath);
                Console.WriteLine($"[PAAB] Replaced path ({path}) in file with {replacedPath}");

                var replacer = new AssetsReplacerFromMemory(0, assetFileInfoEx.index, (int)assetFileInfoEx.curFileType, /*assetFileInfoEx.scriptIndex*/0xffff, baseField.WriteToByteArray());
                _assetsReplacers.Add(replacer);
            } catch (Exception e) {
                Console.WriteLine(e);
                SetError(e);
            }
        }

        internal void ReplaceWorldId() 
        {
            if (Error) return;

            var assetFileInfosEx = _currentFile.table.assetFileInfo;
            if (assetFileInfosEx is null or { Length: <= 0 })
            {
                SetError(new NullReferenceException("AssetsFileInfos is null or empty."));
                return;
            }

            for (int i = 0; i < assetFileInfosEx.Length; i++)
            {
                var assetFileInfoEx = assetFileInfosEx[i];
                try 
                {
                    var cdbType = AssetHelper.FindAssetClassByID(_assetsManager.classFile, assetFileInfoEx.curFileType);
                    if (cdbType == null) continue;

                    var name = AssetHelper.GetAssetNameFast(_currentFile.file, _assetsManager.classFile, assetFileInfoEx);
                    if (string.IsNullOrEmpty(name)) continue;

                    var typeName = cdbType.name.GetString(_assetsManager.classFile);
                    if (string.IsNullOrEmpty(typeName)) continue;

                    if (typeName == "MonoBehaviour") ProcessMonoBehaviour(assetFileInfoEx);
                } 
                catch (Exception e) 
                {
                    Console.WriteLine(e);
                    SetError(e);
                }
            }
        }

        internal void ReplaceCAB()
        {
            var assetFileInfosEx = _currentFile.table.assetFileInfo;
            if (assetFileInfosEx == null || assetFileInfosEx.Length < 1) return;

            for (int i = 0; i < assetFileInfosEx.Length; i++)
            {
                var assetFileInfoEx = assetFileInfosEx[i];
                try
                {
                    var cbdType = AssetHelper.FindAssetClassByID(_assetsManager.classFile, assetFileInfoEx.curFileType);
                    if (cbdType == null) continue;

                    var name = AssetHelper.GetAssetNameFast(_currentFile.file, _assetsManager.classFile, assetFileInfoEx);
                    if (name == null) continue;

                    var basefield = _assetsManager.GetTypeInstance(_currentFile, assetFileInfoEx).GetBaseField();

                    for (int x = 0; x < basefield.childrenCount; x++)
                    {
                        var field = basefield.children[x];
                        if (field.GetFieldType() == "string" && field.GetValue().AsString().Contains("CAB")) 
                        {
                            field.GetValue().Set(field.GetValue().AsString().Replace(_bundleName, _NewbundleName));
                            _assetsReplacers.Add(new AssetsReplacerFromMemory(0, assetFileInfoEx.index, (int)assetFileInfoEx.curFileType, 0xffff, basefield.WriteToByteArray()));
                        }

                        else if (field.childrenCount > 0)
                        {
                            var streamdata = field.GetChildrenList();
                            for (int y = 0; y < streamdata.Length; y++)
                            {
                                var childfield = streamdata[y];
                                if (childfield.GetFieldType() == "string" && childfield.GetValue().AsString().Contains("CAB")) 
                                {
                                    childfield.GetValue().Set(childfield.GetValue().AsString().Replace(_bundleName, _NewbundleName));
                                    _assetsReplacers.Add(new AssetsReplacerFromMemory(0, assetFileInfoEx.index, (int)assetFileInfoEx.curFileType, 0xffff, basefield.WriteToByteArray()));
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        internal void PackAndSave()
        {
            if (Error) return;
            try
            {
                var blockAndDirInfo = _currentBundle.file.bundleInf6;
                var header = _currentBundle.file.bundleHeader6;
                var unpackedHeader = _currentUnpackedFile.bundleHeader6;
                var unpackedBlockAndDirInfo = _currentUnpackedFile.bundleInf6;

                AssetsBundleCompressionType compType = AssetsBundleCompressionType.NONE;
                if (header.totalFileSize > Math.Pow(1024, 2) * 100) compType = AssetsBundleCompressionType.LZMA;
                else if (blockAndDirInfo is { blockCount: >= 1 })
                {
                    compType = blockAndDirInfo.blockInf[0].GetCompressionType() switch
                    {
                        2 => AssetsBundleCompressionType.LZ4,
                        3 => AssetsBundleCompressionType.LZ4HC,
                        _ => AssetsBundleCompressionType.LZMA
                    };
                }
                else
                {
                    SetError(new NullReferenceException("Asset bundle has no blocks."));
                    return;
                }

                List<BundleReplacer> bundleReplacers = new();
                var bundleReplacer = new BundleReplacerFromAssets(_bundleName, _NewbundleName, _currentFile.file, _assetsReplacers, 0);
                bundleReplacers.Add(bundleReplacer);

                for (int i = 0; i < _currentUnpackedFile.bundleInf6.directoryCount; i++)
                {
                    var unpackedDirInfo = _currentUnpackedFile.bundleInf6.dirInf[i];
                    var oldStr = unpackedDirInfo.name;
                    var ext = Path.GetExtension(oldStr);
                    var oldname = Path.GetFileNameWithoutExtension(oldStr);
                    var newCabName = _NewbundleName + ext;
                    bool isAssetsFile = _currentUnpackedFile.IsAssetsFile(_currentUnpackedFile.reader, unpackedDirInfo);

                    if (isAssetsFile) continue;
                    var data = BundleHelper.LoadAssetDataFromBundle(_currentUnpackedFile, i);
                    bundleReplacers.Add(new BundleReplacerFromMemory(oldStr, newCabName, true, data, data.Length, i));
                }

                using var memoryStream = new MemoryStream();
                using var assetsFileWriter = new AssetsFileWriter(memoryStream);
                if (!_currentUnpackedFile.Write(assetsFileWriter, bundleReplacers))
                {
                    SetError(new InvalidOperationException("Unable to write assets file."));
                    return;
                }
                using var reader = new AssetsFileReader(memoryStream);

                Console.WriteLine($"Compression type: {compType} - Estimated time: {GetEstimatedTimeForCompression(unpackedHeader.totalFileSize, compType)}");
                bool wasSuccessful = false;
                if (_saveToFile)
                {
                    AssetsFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + Path.GetExtension(_assetsFilePath));
                    using var fileStream = File.Open(AssetsFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                    using var fileWriter = new AssetsFileWriter(fileStream);
                    wasSuccessful = _currentUnpackedFile.Pack(reader, fileWriter, compType);
                }
                else
                {
                    AssetsFile = new MemoryStream();
                    wasSuccessful = _currentUnpackedFile.Pack(reader, new AssetsFileWriter(AssetsFile), compType);
                }

                if (!wasSuccessful) SetError(new InvalidOperationException("Packing the file wasn't successful."));
            }
            catch (Exception e)
            {
                SetError(e);
            }
        }

        private string GetEstimatedTimeForCompression(long size, AssetsBundleCompressionType compType) 
        {
            var dtn = DateTime.Now;
            TimeSpan timeSpan = TimeSpan.Zero;
            switch (compType) 
            {
                case AssetsBundleCompressionType.NONE:
                    return "instant";
                case AssetsBundleCompressionType.LZMA:
                    timeSpan = new TimeSpan(0, 0, 0, (int) (size / (Math.Pow(1024, 2) * 1.9)));
                    break;
                case AssetsBundleCompressionType.LZ4:
                    timeSpan = new TimeSpan(0, 0, 0, (int) (size / (Math.Pow(1024, 2) * 300)));
                    break;
                case AssetsBundleCompressionType.LZ4HC:
                    timeSpan = new TimeSpan(0, 0, 0, (int) (size / (Math.Pow(1024, 2) * 31)));
                    break;
            }

            return $"{timeSpan:g} ({dtn:T} - {(dtn + timeSpan):T})";
        }

        private void SetError(Exception e) 
        {
            Error = true;
            LastError = e;
        }

        internal enum AssetsToolsObjectType : byte 
        {
            None,
            Avatar,
            World
        }
    }
}
