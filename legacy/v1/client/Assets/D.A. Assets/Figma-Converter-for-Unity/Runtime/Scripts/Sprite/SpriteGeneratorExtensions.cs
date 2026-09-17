using UnityEngine;

namespace DA_Assets.FCU
{
    public static class SpriteGeneratorExtensions
    {
        public static void SetToObject(this Camera camera, GameObject target)
        {
            target.transform.position = new Vector3((int)target.transform.position.x, (int)target.transform.position.y, (int)target.transform.position.z);

            Renderer objectRenderer = target.GetComponent<Renderer>();
            Vector3 objectSize = objectRenderer.bounds.size;
            Vector3 objectPosition = objectRenderer.bounds.center;

            camera.transform.position = new Vector3(objectPosition.x, objectPosition.y, -1);
            camera.orthographicSize = Mathf.Max(objectSize.x / (2f * camera.aspect), objectSize.y / 2f);
        }
    }
}

