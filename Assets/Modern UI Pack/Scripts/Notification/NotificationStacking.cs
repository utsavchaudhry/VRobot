using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Michsky.MUIP
{
    [AddComponentMenu("Modern UI Pack/Notification/Notification Stacking")]
    public class NotificationStacking : MonoBehaviour
    {
        [Header("Settings")]
        public float delay = 1;

        // Helpers
        List<NotificationManager> notifications = new List<NotificationManager>();
        int currentNotification = 0;
        bool enableUpdating = false;

        void Update()
        {
            if (notifications.Count == 0)
                return;

            if (enableUpdating && notifications[currentNotification] != null)
            {
                notifications[currentNotification].Open();

                StopCoroutine("StartNotification");
                StartCoroutine("StartNotification");

                enableUpdating = false;
            }
        }

        public void AddToStack(NotificationManager notif)
        {
            notifications.Add(notif);
            notif.gameObject.SetActive(false);
            enableUpdating = true;
        }

        IEnumerator StartNotification()
        {
            yield return new WaitForSecondsRealtime(notifications[currentNotification].timer + delay);
           
            Destroy(notifications[currentNotification].gameObject);

            if (currentNotification == notifications.Count - 1)
            {
                notifications.Clear();
                currentNotification = 0;
            }

            else
            {
                currentNotification += 1;
                enableUpdating = true;
            }
        }
    }
}