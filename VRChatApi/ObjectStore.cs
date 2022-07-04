using System.Collections.Generic;

namespace LunarUploader.VRChatApi 
{
    
    public class ObjectStore 
    {
        private readonly Dictionary<string, object> _store;

        public ObjectStore(int size)
        {
            _store = new Dictionary<string, object>(size);
        }

        public object this[string key] 
        {
            get => _store[key];
            set => _store[key] = value;
        }

        public bool ContainsKey(string key) 
        {
            return _store.ContainsKey(key);
        }
    }
}