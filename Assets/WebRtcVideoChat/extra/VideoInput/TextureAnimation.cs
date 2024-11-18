using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Byn.Unity.Examples
{
    public class TextureAnimation : MonoBehaviour
    {
        public float _Speed = 0.1f;
        Renderer rend;
        void Start()
        {
            rend = GetComponent<Renderer>();
        }

        // Update is called once per frame
        void Update()
        {
            float offset = Time.time * _Speed;
            rend.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }
    }
}