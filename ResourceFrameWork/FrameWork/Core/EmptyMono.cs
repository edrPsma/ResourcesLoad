using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EG.Resource.Core
{
    public class EmptyMono : MonoBehaviour
    {
        private static EmptyMono mInstance;
        public static EmptyMono Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new GameObject("EmptyMono").AddComponent<EmptyMono>();
                    DontDestroyOnLoad(mInstance.gameObject);
                }
                return mInstance;
            }
        }
    }
}
