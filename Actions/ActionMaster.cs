using UnityEngine;

namespace WManager
{
    internal class ActionMaster : MonoBehaviour 
    {
        private static ActionMaster instance;

        public static ActionMaster Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("WManager-Action").AddComponent<ActionMaster>();
                    DontDestroyOnLoad(instance);
                }
                return instance;
            }
        }
    }
}