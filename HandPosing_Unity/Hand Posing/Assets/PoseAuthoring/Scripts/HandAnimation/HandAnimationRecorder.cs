using UnityEngine;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System;
using Oculus.Platform;
using PoseAuthoring.HandAnimation.VectorGraph;
using Zenject;
using PoseAuthoring.HandAnimation.Interfaces;
using PoseAuthoring.HandAnimation.AnimationGenerators;
using static OVRHand;

namespace PoseAuthoring.HandAnimation
{
    public partial class HandAnimationRecorder : MonoBehaviour
    {
        // Hand Animator
        [SerializeField]
        public HandProvider handProvider;

        [SerializeField]
        public HandTrackTarget HandTrackSelection = HandTrackTarget.Left;

        [SerializeField]
        public bool ShowInBox = true;

        [SerializeField]
        public AnimationPreviewBox previewBox;

        [SerializeField]
        public AnimationVectorAnalyser vectorAnalyser;

        [SerializeField]
        private HandPuppet puppetHand;

        public Animation lastAnimation;

        [SerializeField]
        private KeyCode recordKey = KeyCode.Space;
        private KeyCode replayKey = KeyCode.RightShift;
        private KeyCode deleteKey = KeyCode.Delete;

        IHandAnimationCaptureService _capSvc;
        IHandAnimationGenerater _gen;
        private HandGhost AnimationGhost;
        private int clipCount = 0;
        public string clipName;

        //private Dictionary<double, HandSnapPose> frames = new Dictionary<double, HandSnapPose>();

        public enum HandTrackTarget
        {
            Left,
            Right,
            Both
        }

        [Inject]
        public void Construct(IHandAnimationCaptureService captureService)
        {
            _capSvc = captureService;
        }
        private void Start()
        {

        }

        private void Update()
        {

            if (SelectedHandCheck(puppetHand))
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    AddAnimationPose(puppetHand);
                }
                if (Input.GetKeyUp(recordKey))
                {
                    Debug.Log("recordingKeyUp");
                    _capSvc.ToggleRecordingState();

                }
                if (Input.GetKeyUp(deleteKey) && AnimationGhost != null)
                {
                    lastAnimation = null;
                    DestroyImmediate(AnimationGhost);
                    _capSvc.ClearFrames();
                }
                if (Input.GetKeyUp(replayKey) && lastAnimation != null)
                {
                    lastAnimation.Play();
                }
                DoRecording();
            }
            else
            {
                var hand = puppetHand.trackedHand.GetComponent<OVRHand>();
                if (hand != null)
                {
                    bool isIndexFingerPinching = hand.GetFingerIsPinching(HandFinger.Index);
                    
                    if (hand.IsDataHighConfidence  && isIndexFingerPinching)
                    {
                        _capSvc.ToggleRecordingState();

                        Debug.Log(string.Format("pinch = {0}, hand confidence = {1}, finger confidence:{0}", isIndexFingerPinching.ToString(), hand.HandConfidence, hand.GetFingerConfidence(HandFinger.Index)));
                    }
                }
            }
        }

        private void DoRecording()
        {
            if (_capSvc.GetRecordingState() == Assets.PoseAuthoring.Scripts.HandAnimation.Models.ERecordingState.FinishedRecording && AnimationGhost != null)
            {
                _capSvc.ToggleRecordingState();
                AnimationGhost.gameObject.AddComponent<GenerateFromPoseWithTime>();
                _gen = AnimationGhost.GetComponent<GenerateFromPoseWithTime>();
                lastAnimation =_gen.GenerateAnimations(_capSvc, handProvider, AnimationGhost, clipCount, vectorAnalyser);
                clipName = clipCount.ToString();
                clipCount++;
            }
            else
            {
                RecordPose();
            }
        }

        private bool SelectedHandCheck(HandPuppet puppet)
        {
            HandSnapPose pose = puppet.CurrentPoseVisual(this.transform);

            // only capture and instsiate the hand selection choice 
            if ((HandTrackSelection == HandTrackTarget.Right && pose.handeness == HandSnapPose.Handeness.Right) || (HandTrackSelection == HandTrackTarget.Left && pose.handeness == HandSnapPose.Handeness.Left) || HandTrackSelection == HandTrackTarget.Both)
            {
                return true;
            }
            return false;
        }

        private void AddAnimationPose(HandPuppet puppet)
        {
            HandSnapPose pose = puppet.CurrentPoseVisual(this.transform);
            HandGhost ghost;
            if (AnimationGhost == null)
            {
                float scaleMultiplier = 1f;
                var initPosition = this.transform.position;
                if (ShowInBox)
                {
                    // instance in center of box
                    initPosition = previewBox.GetComponent<Renderer>().bounds.center;
                    // double size
                    scaleMultiplier = 2f;
                    // add reference to boxhandler for binding to ui controls
                    previewBox.a = this;
                }

                ghost = Instantiate(handProvider.GetHand(pose.handeness), initPosition, this.transform.rotation);

                ghost.transform.localScale = ghost.transform.localScale * scaleMultiplier;

                ghost.SetPose(pose, ghost.transform);
                _capSvc.AddFrame(puppet.CurrentPoseVisual(this.transform));
                AnimationGhost = ghost;
            }
            else
            {
                if (_capSvc.GetRecordingState() == Assets.PoseAuthoring.Scripts.HandAnimation.Models.ERecordingState.Recording)
                {
                    AnimationGhost.SetPose(pose, AnimationGhost.transform);
                    _capSvc.AddFrame(puppet.CurrentPoseVisual(this.transform));
                }
            }
        }

        public void RecordPose()
        {
            AddAnimationPose(puppetHand);
        }
    }
}