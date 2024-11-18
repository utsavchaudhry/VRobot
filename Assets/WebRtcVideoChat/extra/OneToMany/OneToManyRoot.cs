/* 
 * Copyright (C) 2021 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */
using UnityEngine;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// Root UI element. Sender & Receiver UI elements will be added to this
    /// </summary>
    public class OneToManyRoot : MonoBehaviour
    {
        public GameObject ReceiverClone;


        public void AddReceiver()
        {
            Object.Instantiate(ReceiverClone, Vector2.zero, Quaternion.identity, this.GetComponent<RectTransform>());
        }
        public void AddSender()
        {
            var sender = Object.Instantiate(ReceiverClone, Vector2.zero, Quaternion.identity, this.GetComponent<RectTransform>());
            sender.GetComponent<OneToMany>().uSender = true;
        }
    }

}