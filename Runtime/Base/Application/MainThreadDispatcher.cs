using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Eidetic.URack
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        static MainThreadDispatcher instance;
        static MainThreadDispatcher Instance => instance ??
            (instance = new GameObject("MainThreadDispatcher").AddComponent<MainThreadDispatcher>());

        Queue<Action> ActionQueue = new Queue<Action>();
        public static void Enqueue(Action action) => Instance.ActionQueue.Enqueue(action);

        bool RunningActions = false;

        IEnumerator RunActions()
        {
            RunningActions = true;

            var timer = new Stopwatch();
            timer.Start();
            bool restartTimer = false;

            while (ActionQueue.Count != 0)
            {
                if (restartTimer)
                {
                    timer.Restart();
                    restartTimer = false;
                }

                var action = ActionQueue.Dequeue();
                action.Invoke();

                // only spend a max of 8 miliseconds running queued actions
                // that's around half a 60fps frame
                if (timer.ElapsedMilliseconds > 8)
                {
                    restartTimer = true;
                    yield return null;
                }
            }

            RunningActions = false;
        }

        void Update()
        {
            if (!RunningActions && ActionQueue.Count != 0)
                StartCoroutine("RunActions");
        }
    }
}