using UnityEngine;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System;
using Oculus.Platform;
using PoseAuthoring.HandAnimation.Interfaces;
using Assets.PoseAuthoring.Scripts.HandAnimation.Models;

namespace PoseAuthoring.HandAnimation.CaptureServices
{
    public class SmoothOverTimeHandCapureService : IHandAnimationCaptureService
    {
        public Dictionary<double, HandSnapPose> Frames { get => frames; }

        public ERecordingState Recording = ERecordingState.Norecording;

        // Internal
        private Dictionary<double, HandSnapPose> frames = new Dictionary<double, HandSnapPose>();
        private System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        private DateTime lastFired;

        public void ToggleRecordingState()
        {
            if (lastFired == null || (DateTime.UtcNow - lastFired).TotalSeconds > 1)
            {
                switch (Recording)
                {
                    case ERecordingState.Recording:
                        {
                            Recording = ERecordingState.FinishedRecording;
                            timer.Stop();
                            Debug.Log(string.Format("{0} frames in {2} seconds =  captured at {1} fps", frames.Count, frames.Count / timer.Elapsed.TotalSeconds, timer.Elapsed.TotalSeconds));
                            break;
                        }
                    case ERecordingState.FinishedRecording:
                        {
                            Recording = ERecordingState.Norecording;
                            break;
                        }
                    case ERecordingState.Norecording:
                        {
                            timer.Start();
                            Recording = ERecordingState.Recording;
                            break;
                        }
                }
                lastFired = DateTime.UtcNow;
            }
        }

        public void AddFrame(HandSnapPose handSnapPose)
        {
            if (Recording == ERecordingState.Recording)
            {
                if (!frames.ContainsKey(timer.Elapsed.TotalSeconds))
                { frames.Add(timer.Elapsed.TotalSeconds, handSnapPose); }
            }
        }

        public ERecordingState GetRecordingState()
        {
            return Recording;
        }

        public void ClearFrames()
        {
            frames = new Dictionary<double, HandSnapPose>();
        }
    }
}