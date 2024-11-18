/* 
 * Copyright (C) 2022 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using UnityEngine;
using UnityEngine.UI;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// This will resize the video screen for different aspect ratios.
    /// Requirements for the GameObject that uses this component:
    /// * needs to have a RawImage component + a valid texture
    /// * Object needs to have a RectTransform (used to change the size of the image)
    /// * Parent object also needs a RectTransform (used to get the max available space for the image)
    /// </summary>
    public class ImgFitter : MonoBehaviour
    {

        // Update is called once per frame
        void Update()
        {
            RawImage image = GetComponent<RawImage>();
            RectTransform ltransform = GetComponent<RectTransform>();
            if (ltransform == null || image == null || image.texture == null)
                return;
            RectTransform ptransform = ltransform.parent.GetComponent<RectTransform>();
            if (ptransform == null)
                return;

            Vector2 parentSize = new Vector2(ptransform.rect.width, ptransform.rect.height);

            Vector2 availableDelta = (ltransform.localRotation) * parentSize;
            availableDelta = Abs(availableDelta);

            float heightScale = 1;

            if (image.material != null && image.material.name == "I420_one_buffer")
            {
                //if we use the i420p stored in a single buffer the height
                //of the image is 50% larger to store the u & v planes.
                //They aren't rendered though so the actual image is just 66% of
                //the textures height
                heightScale = 2.0f / 3.0f;
            }

            int width = image.texture.width;
            int height = (int)(image.texture.height * heightScale);
            float ratio = width / (float)height;

            Vector2 res = new Vector2();
            if (availableDelta.x / width < availableDelta.y / height)
            {
                res.x = availableDelta.x;
                res.y = availableDelta.x / ratio;
            }
            else
            {
                res.x = availableDelta.y * ratio;
                res.y = availableDelta.y;
            }
            ltransform.sizeDelta = res;
            ltransform.anchorMin = new Vector2(0.5f, 0.5f);
            ltransform.anchorMax = new Vector2(0.5f, 0.5f);
            ltransform.pivot = new Vector2(0.5f, 0.5f);

        }

        Vector2 Abs(Vector2 v)
        {
            v.x = Mathf.Abs(v.x);
            v.y = Mathf.Abs(v.y);
            return v;
        }
    }
}