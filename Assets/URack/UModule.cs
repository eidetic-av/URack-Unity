using System.Collections.Generic;
using UnityEngine;

namespace Eidetic.URack
{
    public abstract class UModule : MonoBehaviour
    {
        public static Dictionary<int, UModule> Instances { get; private set; }  = new Dictionary<int, UModule>();

        public static UModule Create(string moduleType, int id)
        {
            var prefab = Resources.Load<GameObject>(moduleType + "Prefab");
            var gameObject = Instantiate(prefab);
            gameObject.name = moduleType + "Instance" + id;
            var moduleInstance = (UModule) gameObject.GetComponent(moduleType);
            moduleInstance.ModuleType = moduleType;
            moduleInstance.Id = id;
            Instances[id] = moduleInstance;
            return moduleInstance;
        }

        public static void Remove(int id)
        {
            Destroy(Instances[id].gameObject);
            Instances.Remove(id);
        }

        public int Id { get; private set; }
        public string ModuleType { get; private set; }
    }
}