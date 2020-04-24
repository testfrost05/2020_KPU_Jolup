using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VrFps
{   //부착아이템 매니저
    public class AttachmentManager : MonoBehaviour 
    {
        protected AttachmentSocket[] sockets = new AttachmentSocket[0]; //소켓 배열생성

        public delegate void OnAttach(Attachment attachment); //OnAttach 
        public OnAttach _OnAttach;

        public delegate void OnDetach(Attachment attachment); //OnDetach
        public OnDetach _OnDetach;

        void Start()
        {
            sockets = GetComponentsInChildren<AttachmentSocket>();

            if (sockets == null)//소켓없음 없앰
            {
                Destroy(this);
            }

            foreach (AttachmentSocket socket in sockets) //소켓마다 반복
            {
                socket._OnAttach += AttachWrap;
                socket._OnDetach += DetachWrap;
            }

            ToggleAttach(true);
        }

        void ToggleAttach(bool enable)
        {
            foreach (AttachmentSocket socket in sockets)
            {
                if (socket.HasItem)
                {
                    if (enable)
                    {
                        socket.EnableAttachment();
                    }
                    else
                    {
                        socket.DisableAttachment();
                    }
                }
            }
        }

        void AttachWrap(Attachment attachment)
        {
            if (_OnAttach != null)
            {
                _OnAttach(attachment);
            }
        }

        void DetachWrap(Attachment attachment)
        {
            if (_OnDetach != null)
            {
                _OnDetach(attachment);
            }
        }
    }
}