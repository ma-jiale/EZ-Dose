using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class WindowInstanceCache : ScriptableObject
{
    [System.Serializable]
    public class InstanceEntry
    {
        public Editor Key;
        public EditorWindow Value;
    }

    [HideInInspector]
    public List<InstanceEntry> Instances = new List<InstanceEntry>();

    private const int MAX_INSTANCES = 100;

    public void Add(Editor key, EditorWindow value)
    {
        if (key == null)
        {
            return;
        }

        Cleanup();

        Instances.RemoveAll(entry => entry.Key == key);

        InstanceEntry newEntry = new InstanceEntry
        {
            Key = key,
            Value = value
        };

        Instances.Add(newEntry);

        if (Instances.Count > MAX_INSTANCES)
        {
            if (Instances[0].Value != null)
            {
                DestroyImmediate(Instances[0].Value);
            }
            Instances.RemoveAt(0);
        }

        EditorUtility.SetDirty(this);
    }

    public bool TryGetValue(Editor key, out EditorWindow value)
    {
        if (key == null)
        {
            value = null;
            return false;
        }

        var entry = Instances.FirstOrDefault(e => e.Key == key);

        if (entry != null && entry.Value != null)
        {
            value = entry.Value;
            return true;
        }

        value = null;
        return false;
    }

    public void Cleanup()
    {
        int removedCount = Instances.RemoveAll(e => e.Key == null || e.Value == null);

        if (removedCount > 0)
        {
            EditorUtility.SetDirty(this);
        }
    }
}