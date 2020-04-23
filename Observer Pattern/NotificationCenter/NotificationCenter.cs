using System.Collections.Generic;

namespace CodeComplete.Observer_Pattern.NotificationCenter
{
    public class NotificationCenter
    {
        public delegate bool NotificationHandler(string notification, object sender,
            Dictionary<string, object> moreInfo);

        private readonly Dictionary<string, List<RegisterInfo>> subscriptionInfo =
            new Dictionary<string, List<RegisterInfo>>();

        public int GetRegisterCount(string notification)
        {
            if (subscriptionInfo.ContainsKey(notification))
                return subscriptionInfo[notification].Count;
            return 0;
        }

        public int RemoveNotification(NotificationHandler handler)
        {
            int num = 0;
            foreach (var pair in subscriptionInfo)
                for (int i = pair.Value.Count - 1; i >= 0; i--)
                    if (pair.Value[i].Handler == handler)
                    {
                        pair.Value.RemoveAt(i);
                        num++;
                    }
            return num;
        }

        public void RemoveNotification(string notification)
        {
            if (subscriptionInfo.ContainsKey(notification))
            {
                subscriptionInfo[notification].Clear();
                subscriptionInfo.Remove(notification);
            }
        }

        public int RemoveNotification(string notification, NotificationHandler handler)
        {
            int num = 0;
            if (subscriptionInfo.ContainsKey(notification))
            {
                var list = subscriptionInfo[notification];
                for (int i = list.Count - 1; i >= 0; i--)
                    if (list[i].Handler == handler)
                    {
                        list.RemoveAt(i);
                        num++;
                    }
            }
            return num;
        }

        public int RemoveNotification(string notification, object sender, NotificationHandler handler)
        {
            int num = 0;
            if (subscriptionInfo.ContainsKey(notification))
            {
                var list = subscriptionInfo[notification];
                for (int i = list.Count - 1; i >= 0; i--)
                    if (list[i].Handler == handler && list[i].Sender == sender)
                    {
                        list.RemoveAt(i);
                        num++;
                    }
            }
            return num;
        }

        public void RemoveAllNotifications()
        {
            foreach (var pair in subscriptionInfo)
                pair.Value.Clear();
            subscriptionInfo.Clear();
        }

        public void AddNotification(string notification, object sender, NotificationHandler handler)
        {
            if (!subscriptionInfo.ContainsKey(notification))
                subscriptionInfo.Add(notification, new List<RegisterInfo>());
            var item = new RegisterInfo(sender, handler);
            subscriptionInfo[notification].Add(item);
        }

        public void PostNotification(string notification, object sender, Dictionary<string, object> moreInfo)
        {
            if (subscriptionInfo.ContainsKey(notification))
                foreach (var info in subscriptionInfo[notification])
                    if ((info.Sender == null || info.Sender == sender) && !info.Handler(notification, sender, moreInfo))
                        break;
        }

        private class RegisterInfo
        {
            public readonly NotificationHandler Handler;
            public readonly object Sender;

            public RegisterInfo(object sender, NotificationHandler handler)
            {
                Sender = sender;
                Handler = handler;
            }
        }
    }
}
