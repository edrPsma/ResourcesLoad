using EG.Resource.Core;

namespace EG.Resource
{
    public class ResourceFrameWork : Singleton<ResourceFrameWork>
    {
        public void Init(bool inEditor)
        {
            ResourcesManager.Instance.Init(inEditor);
            ObjectPoolManager.Instance.Init();
        }
    }
}
