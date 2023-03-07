using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EG.Resource.Core;

namespace EG.Resource
{
    public partial class ObjectPoolManager
    {
        /// <summary>
        /// 预加载游戏物体
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="count">数量</param>
        /// <param name="clear">跳场景是否清空缓存</param>
        public void PreloadGameObject(string path, int count = 1, bool clear = false)
        {
            if (string.IsNullOrEmpty(path)) return;

            uint crc = CRC32.GetCRC32(path);
            GameObject obj = ResourcesManager.Instance.Load<GameObject>(path, clear);
            if (obj == null)
            {
                Debug.LogError("预加载失败,path:" + path);
                return;
            }
            ResourcesManager.Instance.AddRefCount(crc, -1);// 因为是预加载,不计引用

            if (!mGameObjectPoolDic.ContainsKey(crc))
            {
                mGameObjectPoolDic.Add(crc, new List<ObjectItem>());
            }
            List<ObjectItem> objectItemList = mGameObjectPoolDic.TryGet(crc);
            for (int i = 0; i < count; i++)
            {
                GameObject gameObject = GameObject.Instantiate(obj, RecycleNode, true);
#if UNITY_EDITOR
                gameObject.name += "(Recycle)";
#endif

                // 添加进缓存
                int guid = gameObject.GetInstanceID();
                ObjectItem objectItem = mGameObjectItemPool.Spawn(true);
                objectItem.CRC = crc;
                objectItem.Clear = clear;
                objectItem.GameObject = gameObject;
                objectItem.GUID = guid;
                objectItem.IsInPool = true;
                mGuidDic.Add(guid, objectItem);
                objectItemList.Add(objectItem);
            }
        }
    }
}
