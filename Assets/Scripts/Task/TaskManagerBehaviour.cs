using System.Collections;
using System.Linq;
using NBackStreaming;
using UnityEngine;
using UnityEngine.UI;

namespace Task
{
    /// <summary>
    /// Example behaviour that uses the <see cref="TextTaskManager"/>.
    /// </summary>
    public class TaskManagerBehaviour : MonoBehaviour
    {
        private TextTaskManager _manager = new TextTaskManager();
        private Text _text;

        /// <summary>
        /// The <see cref="TextTaskManager"/> driving the queued tasks.
        /// </summary>
        public TextTaskManager Manager => _manager;

        /// <summary>
        /// The UI element the running task output is written to.
        /// </summary>
        public Text DisplayText => _text;

        /// <summary>
        /// Input handler.
        /// </summary>
        public void HandleOnInput()
        {
            if (_manager == null)
                return;

            _manager.HandleOnInput();

            // example for getting nbacktask properties
            if (_manager.RunningTask is NBackTask nbackTask)
            {
                if (!nbackTask.IsCorrect)  // possibility to show that nbacktask answer is wrong.
                    StartCoroutine(ChangeTextColor(nbackTask.CharacterRemainingTime));

                Debug.Log(nbackTask.ReactionTime); // get reaction time

                RaiseNBackResponse(nbackTask);
            }
            else
            {
                RaiseNonNBackResponse(_manager.RunningTask);
            }
        }

        #region Unity methods
        private void Start()
        {
            _text = GetComponentInChildren<Text>(); // get UI element for text based tasks

            var taskBehaviours = GetComponentsInChildren<TextTaskBehaviour>(); // get all text based tasks behaviours

            _manager.OnTaskFinished += HandleOnTaskFinished; // possibility to save data after tasks are finished
            _manager.EnqueueTasks(taskBehaviours.Select(x => x.Task).ToArray()); // extract tasks
            _manager.PrepareTask(); // begin with first task

            NBackEvents.RaiseSessionStarted(taskBehaviours.Length);
        }

        private void Update() => _text.text = _manager.Run(Time.deltaTime);
        #endregion

        #region Private methods
        private void HandleOnTaskFinished(ATask<string> task)
        {
            if (!(task is NBackTask nBackTask))
                return;
            Debug.Log(nBackTask.ErrorRate); // for example save error rate
        }

        private IEnumerator ChangeTextColor(float wait)
        {
            _text.color = Color.red;
            yield return new WaitForSeconds(wait);
            _text.color = Color.black;
        }

        private void RaiseNBackResponse(NBackTask nbackTask)
        {
            bool isTarget = nbackTask.IsTargetAt(nbackTask.Index);
            bool stimulusPhase = !string.IsNullOrEmpty(nbackTask.Character);

            var response = new ResponseData
            {
                Kind = TaskKind.NBack,
                TrialIndex = nbackTask.Index,
                Letter = nbackTask.CharacterSet[nbackTask.Index],
                IsTarget = isTarget,
                Phase = stimulusPhase ? "stimulus" : "isi",
                HasRt = true,
                Classification = isTarget ? TrialOutcomeKind.Hit : TrialOutcomeKind.FalseAlarm
            };

            if (stimulusPhase)
            {
                response.RtStimulus = nbackTask.ReactionTime;
                response.RtTrial = nbackTask.ReactionTime;
            }
            else
            {
                // Press during the ISI, measured from stimulus onset of the current trial.
                // The press happened after stimulus offset, so the stimulus-locked and the
                // trial-locked reaction times are the same trial-relative value.
                float rtTrial = nbackTask.StimulusDuration +
                    (nbackTask.InterStimulusDuration - nbackTask.CharacterRemainingTime);
                response.RtStimulus = rtTrial;
                response.RtTrial = rtTrial;
            }

            NBackEvents.RaiseResponse(response);
        }

        private void RaiseNonNBackResponse(ATask<string> task)
        {
            var kind = TaskKind.Unknown;
            if (task is InfoTask)
                kind = TaskKind.Info;
            else if (task is FocusTask)
                kind = TaskKind.Focus;

            NBackEvents.RaiseResponse(new ResponseData
            {
                Kind = kind,
                TrialIndex = -1,
                Letter = string.Empty,
                IsTarget = false,
                Phase = "na",
                RtStimulus = 0f,
                RtTrial = 0f,
                HasRt = false,
                Classification = TrialOutcomeKind.NA
            });
        }
        #endregion
    }
}
