using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace EG.Resource.Core
{
    public class ABBuilder
    {
        #region 参数
        private static Dictionary<string, string> mAllFileDir = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> mAllPrefabDir = new Dictionary<string, List<string>>();
        private static List<string> mAllFileAB = new List<string>();
        private static List<string> mAllValidPaths = new List<string>();

        static string ABTargetPath = Application.dataPath + "/../AssetBundle";
        static string XMLPATH = Application.dataPath + "/AssetBundleConfig.xml";
        static string BYTEPATH = Application.dataPath + "/AssetBundleConfig.bytes";
        private static string mABConfigName;
        private static BuildingConfig buildingConfig;
        #endregion

        [MenuItem("RED/2.打AB包", false, 1)]
        public static void ClickBuildBtn()
        {
            BeforeBuild();
            Build();
            AfterBuild();
            BuildOver();
        }

        #region 打包流程
        static void BeforeBuild()
        {
            LoadConfig();
            SetAllABName();
        }

        static void Build()
        {
            string[] allBundes = AssetDatabase.GetAllAssetBundleNames();
            Dictionary<string, string> resPathDic = new Dictionary<string, string>();
            for (int i = 0; i < allBundes.Length; i++)
            {
                string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundes[i]);
                for (int j = 0; j < allBundlePath.Length; j++)
                {
                    if (allBundlePath[j].EndsWith(".cs"))
                    {
                        continue;
                    }
                    if (CheckValidPath(allBundlePath[j]))
                    {
                        resPathDic.Add(allBundlePath[j], allBundes[i]);
                    }
                }
            }

            DeleteUnUseAB();
            //生成XML配置文件和二进制文件
            CreateConfig(resPathDic);
            AssetDatabase.Refresh();

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles
            (
                ABTargetPath,
                BuildAssetBundleOptions.ChunkBasedCompression,
                EditorUserBuildSettings.activeBuildTarget
            );

            if (manifest == null)
            {
                Debug.LogError("AB包打包失败");
            }
            else
            {
                Debug.Log("AB包打包成功");
            }
        }

        static void AfterBuild()
        {
            DeleteAllABName();
            SetABName("", BYTEPATH.Replace(Application.dataPath, "Assets"));

            if (File.Exists(BYTEPATH))
            {
                File.Delete(BYTEPATH);
                File.Delete(BYTEPATH + ".meta");
            }

            Resources.UnloadAsset(buildingConfig);
            buildingConfig = null;

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        static void BuildOver()
        {
            mAllFileDir?.Clear();
            mAllPrefabDir?.Clear();
            mAllFileAB?.Clear();
            mAllValidPaths?.Clear();
        }

        static void LoadConfig()
        {
            buildingConfig = Resources.Load<BuildingConfig>("ABConfig");
            mABConfigName = buildingConfig.ConfigurationFileABPackageName;
            if (!Directory.Exists(ABTargetPath))
            {
                Directory.CreateDirectory(ABTargetPath);
            }

            #region 处理文件夹
            foreach (var item in buildingConfig.AllDirectoryPath)
            {
                if (mAllFileDir.ContainsKey(item.name))
                {
                    Debug.LogError("AB包配置文件重复,打包终止,name:" + item.name);
                    return;
                }
                else
                {
                    mAllFileDir.Add(item.name, item.path);
                    mAllFileAB.Add(item.path);
                    mAllValidPaths.Add(item.path);
                }
            }
            #endregion

            #region 处理预制体
            string[] allPrefabPath = buildingConfig.AllPrefabPath.ToArray();
            if (allPrefabPath.Length != 0)
            {
                string[] allPrefabGUID = AssetDatabase.FindAssets("t:Prefab", allPrefabPath);
                for (int i = 0; i < allPrefabGUID.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(allPrefabGUID[i]);
                    EditorUtility.DisplayProgressBar("查找预制体", "Prefab:" + path, i * 1.0f / allPrefabGUID.Length);
                    mAllValidPaths.Add(path);

                    if (!ContainAllFileAB(path))
                    {
                        GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        string[] allDepend = AssetDatabase.GetDependencies(path);
                        List<string> allDependPath = new List<string>();
                        for (int j = 0; j < allDepend.Length; j++)
                        {
                            if (!ContainAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs"))
                            {
                                mAllFileAB.Add(allDepend[j]);
                                allDependPath.Add(allDepend[j]);
                            }
                        }

                        if (mAllPrefabDir.ContainsKey(obj.name))
                        {
                            Debug.LogError("存在相同名字的Prefab,name:" + obj.name);
                        }
                        else
                        {
                            mAllPrefabDir.Add(obj.name, allDependPath);
                        }
                    }
                }
            }
            #endregion
        }
        #endregion

        private static bool ContainAllFileAB(string path)
        {
            for (int i = 0; i < mAllFileAB.Count; i++)
            {
                if (path == mAllFileAB[i] || path.Contains(mAllFileAB[i]) && (path.Replace(mAllFileAB[i], "")[0] == '/'))
                {
                    return true;
                }
            }

            return false;
        }

        #region 设置AB包名
        private static void SetABName(string abName, string path)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(path);
            if (assetImporter == null)
            {
                Debug.LogError("不存在此路径,path:" + path);
                return;
            }
            else
            {
                assetImporter.assetBundleName = abName;
            }
        }
        private static void SetABName(string abName, List<string> paths)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                SetABName(abName, paths[i]);
            }
        }

        private static void SetAllABName()
        {
            foreach (var name in mAllFileDir.Keys)
            {
                SetABName(name, mAllFileDir[name]);
            }

            foreach (var name in mAllPrefabDir.Keys)
            {
                SetABName(name, mAllPrefabDir[name]);
            }
        }

        private static void DeleteAllABName()
        {
            foreach (var name in mAllFileDir.Keys)
            {
                SetABName("", mAllFileDir[name]);
            }

            foreach (var name in mAllPrefabDir.Keys)
            {
                SetABName("", mAllPrefabDir[name]);
            }

            string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
            for (int i = 0; i < oldABNames.Length; i++)
            {
                AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
                EditorUtility.DisplayProgressBar("清除AB包名", "name:" + oldABNames[i], i * 1.0f / oldABNames.Length);
            }

            //SetABName("", BYTEPATH.Replace(Application.dataPath, "Assets"));
        }
        #endregion

        #region 是否有效路径
        static bool CheckValidPath(string path)
        {
            for (int i = 0; i < mAllValidPaths.Count; i++)
            {
                if (path.Contains(mAllValidPaths[i]))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 删除没用的AB包
        static void DeleteUnUseAB()
        {
            string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
            DirectoryInfo directoryInfo = new DirectoryInfo(ABTargetPath);
            FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (ContainABName(file.Name, allBundlesName) || file.Name.EndsWith(".meta"))
                {
                    continue;
                }
                else
                {
                    if (file.Name == "AssetBundle" || file.Name == "AssetBundle.manifest")
                        continue;

                    if (file.Name == mABConfigName || file.Name == mABConfigName + ".manifest")
                        continue;

                    if (File.Exists(file.FullName) && !file.FullName.EndsWith(".manifest"))
                    {
                        Debug.Log("此AB包已经被删:" + file.Name);
                        File.Delete(file.FullName);
                        File.Delete(file.FullName + ".meta");
                    }
                }
            }
        }

        static bool ContainABName(string name, string[] strs)
        {
            for (int i = 0; i < strs.Length; i++)
            {
                if (name == strs[i] || name == strs[i] + ".manifest")
                    return true;
            }
            return false;
        }
        #endregion

        #region 生成配置表
        static void CreateConfig(Dictionary<string, string> resPathDic)
        {
            ABConfig config = new ABConfig();
            config.ABList = new List<ABBase>();
            foreach (var path in resPathDic.Keys)
            {
                ABBase abBase = new ABBase
                {
                    Path = path,
                    Crc = CRC32.GetCRC32(path),
                    ABName = resPathDic[path],
                    AssetName = path.Remove(0, path.LastIndexOf("/") + 1),
                    ABDependce = new List<string>(),
                };

                string[] resDependce = AssetDatabase.GetDependencies(path);
                for (int i = 0; i < resDependce.Length; i++)
                {
                    string tempPath = resDependce[i];
                    if (tempPath == path || path.EndsWith(".cs"))
                        continue;

                    string abName = "";
                    if (resPathDic.TryGetValue(tempPath, out abName))
                    {
                        if (abName == resPathDic[path])
                            continue;

                        if (!abBase.ABDependce.Contains(abName))
                        {
                            abBase.ABDependce.Add(abName);
                        }
                    }
                }
                config.ABList.Add(abBase);
            }
            if (File.Exists(XMLPATH)) File.Delete(XMLPATH);
            FileStream fileStream = new FileStream(XMLPATH, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
            XmlSerializer xs = new XmlSerializer(config.GetType());
            xs.Serialize(sw, config);
            sw.Close();
            fileStream.Close();

            foreach (var item in config.ABList)
            {
                item.Path = "";//因为二进制文件不需要Path,因此将其清除以减少内存
            }
            FileStream fs = new FileStream(BYTEPATH, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, config);
            fs.Close();
            AssetDatabase.ImportAsset(BYTEPATH.Replace(Application.dataPath, "Assets"));
            SetABName(mABConfigName, BYTEPATH.Replace(Application.dataPath, "Assets"));
        }
        #endregion
    }
}

