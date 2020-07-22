using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace VrFps
{
    public class VrFpsInput
    {
        public delegate void LocalInputEvent();

        public static bool Input(LocalInputEvent localInputEvent, SteamVR_Action_Boolean button, Hand inputDevice)
        {
            bool success = Input(button, inputDevice);

            if (success)
                if (localInputEvent != null)
                    localInputEvent();

            return success;
        }

        public static bool Input(SteamVR_Action_Boolean button, Hand inputDevice)
        {
            if (button == null)
                return false;

            if (!inputDevice)
                return false;

            if (button.GetState(inputDevice.inputSource))
                return true;

            return false;
        }

        public static bool InputDown(LocalInputEvent localInputEvent, SteamVR_Action_Boolean button, Hand inputDevice)
        {
            bool success = InputDown(button, inputDevice);

            if (success)
                if (localInputEvent != null)
                    localInputEvent();

            return success;
        }

        public static bool InputDown(SteamVR_Action_Boolean button, Hand inputDevice)
        {
            if (button == null)
                return false;

            if (!inputDevice)
                return false;

            if (button.GetStateDown(inputDevice.inputSource))
                return true;

            return false;
        }


        public static bool InputUp(LocalInputEvent localInputEvent, SteamVR_Action_Boolean button, Hand inputDevice)
        {
            bool success = InputUp(button, inputDevice);

            if (success)
                if (localInputEvent != null)
                    localInputEvent();

            return success;
        }


        public static bool InputUp(SteamVR_Action_Boolean button, Hand inputDevice)
        {
            if (button == null)
                return false;

            if (!inputDevice)
                return false;

            if (button.GetStateUp(inputDevice.inputSource))
                return true;

            return false;
        }

        public enum TouchPadDirection
        {
            left,
            right,
            up,
            down,
            center,
            dontMatter,
            ignore
        }

        public static bool TouchPadInput(LocalInputEvent touchpadEvent, TouchPadDirection direction, Hand inputDevice)
        { return TouchPadInput(touchpadEvent, direction, inputDevice ? inputDevice.TouchpadAxis : Vector2.zero); }

        public static bool TouchPadInput(LocalInputEvent touchpadEvent, TouchPadDirection direction, Vector2 touchpadAxis)
        {
            if (touchpadEvent == null)
                return false;

            switch (direction)
            {
                case TouchPadDirection.left:
                    if (touchpadAxis.y > -0.75f && touchpadAxis.y < 0.75f)
                        if (touchpadAxis.x < -0.5f)
                        {
                            touchpadEvent();
                            return true;
                        }
                    break;
                case TouchPadDirection.right:
                    if (touchpadAxis.y > -0.75f && touchpadAxis.y < 0.75f)
                        if (touchpadAxis.x > 0.5f)
                        {
                            touchpadEvent();
                            return true;
                        }
                    break;
                case TouchPadDirection.up:
                    if (touchpadAxis.x > -0.75f && touchpadAxis.x < 0.75f)
                        if (touchpadAxis.y > 0.5f)
                        {
                            touchpadEvent();
                            return true;
                        }
                    break;
                case TouchPadDirection.down:
                    if (touchpadAxis.x > -0.75f && touchpadAxis.x < 0.75f)
                        if (touchpadAxis.y < -0.5f)
                        {
                            touchpadEvent();
                            return true;
                        }
                    break;
                case TouchPadDirection.center:
                    if (Vector2.Distance(touchpadAxis, Vector2.zero) < 0.75f)
                    {
                        touchpadEvent();
                        return true;
                    }
                    break;
                case TouchPadDirection.dontMatter:
                    touchpadEvent();
                    return true;
            }

            return false;
        }
    }
}
