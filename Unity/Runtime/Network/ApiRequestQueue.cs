using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Game.Network
{
    public abstract class ApiCommand
    {
        private Action onSuccess;
        private Action<ApiError> onError;

        public void Execute(Action onSuccess, Action<ApiError> onError)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
            ExecuteInternal();
        }

        protected abstract void ExecuteInternal();
        protected void Success() => onSuccess?.Invoke();
        protected void Error(ApiError error) => onError?.Invoke(error);
    }

    public sealed class ApiRequestQueue : MonoBehaviour
    {
        private readonly Queue<ApiCommand> jobs = new Queue<ApiCommand>();
        private bool isProcessing;

        public void Enqueue(ApiCommand command)
        {
            jobs.Enqueue(command);

            if (!isProcessing)
                StartCoroutine(Process());
        }

        private IEnumerator Process()
        {
            isProcessing = true;

            while (jobs.Count > 0)
            {
                bool completed = false;
                ApiError failed = null;
                ApiCommand command = jobs.Dequeue();

                command.Execute(
                    () => completed = true,
                    error =>
                    {
                        failed = error;
                        completed = true;
                    });

                yield return new WaitUntil(() => completed);

                if (failed != null)
                {
                    Debug.LogError($"API command failed: {failed.Code} {failed.Message}");
                    break;
                }
            }

            isProcessing = false;
        }
    }
}
