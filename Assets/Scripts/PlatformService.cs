using UnityEngine;

namespace CardMesh
{
    public abstract class PlatformService : MonoBehaviour
    {
        public virtual void Initialize()
        {
        }

        public virtual void TrackEvent(string eventName)
        {
        }

        public virtual void SaveProgress(string saveKey, string payload)
        {
        }
    }
}
